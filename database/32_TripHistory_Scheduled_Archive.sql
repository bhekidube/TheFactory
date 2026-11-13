IF COL_LENGTH('dbo.TripHistory', 'ActualTimesEstimated') IS NULL
BEGIN
    ALTER TABLE dbo.TripHistory
    ADD ActualTimesEstimated BIT NOT NULL
        CONSTRAINT DF_TripHistory_ActualTimesEstimated DEFAULT (0) WITH VALUES;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TripHistory')
      AND name = 'ArchivedBy'
      AND is_nullable = 0
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TripHistory_ArchivedBy')
    BEGIN
        ALTER TABLE dbo.TripHistory DROP CONSTRAINT FK_TripHistory_ArchivedBy;
    END

    ALTER TABLE dbo.TripHistory ALTER COLUMN ArchivedBy INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TripHistory_ArchivedBy')
BEGIN
    ALTER TABLE dbo.TripHistory WITH CHECK
    ADD CONSTRAINT FK_TripHistory_ArchivedBy FOREIGN KEY (ArchivedBy) REFERENCES dbo.[User](UserId);
END
GO

CREATE OR ALTER PROCEDURE dbo.ArchiveDueTripHistory
    @ServiceDate DATE,
    @LocalNow DATETIME2,
    @UtcOffsetMinutes SMALLINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LockResult INT;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = N'TripHistoryDailyArchive',
        @LockMode = N'Exclusive',
        @LockOwner = N'Session',
        @LockTimeout = 0;

    IF @LockResult < 0
    BEGIN
        SELECT 0 AS ArchivedCount;
        RETURN;
    END

    ;WITH ServiceDates AS (
        SELECT @ServiceDate AS ServiceDate
        UNION ALL
        SELECT DATEADD(DAY, -1, @ServiceDate)
    ), ScheduledOccurrences AS (
        SELECT
            RT.TripId,
            RT.RouteId,
            R.OperatorId,
            D.ServiceDate,
            DATEADD(
                SECOND,
                DATEDIFF(SECOND, CAST('00:00:00' AS TIME), CAST(RT.DepartureDateTime AS TIME)),
                CAST(D.ServiceDate AS DATETIME2)
            ) AS ScheduledDepartureDateTime,
            DATEDIFF(SECOND, RT.DepartureDateTime, RT.ArrivalDateTime) AS ScheduledDurationSeconds
        FROM dbo.RouteTrip RT
        INNER JOIN dbo.Route R ON R.RouteId = RT.RouteId
        CROSS JOIN ServiceDates D
                WHERE RT.Active = 1
                    AND (RT.Notes LIKE '%Daily%'
                             OR CAST(RT.DepartureDateTime AS DATE) = D.ServiceDate)
    ), DueOccurrences AS (
        SELECT
            TripId,
            RouteId,
            OperatorId,
            ServiceDate,
            ScheduledDepartureDateTime,
            DATEADD(SECOND, ScheduledDurationSeconds, ScheduledDepartureDateTime) AS ScheduledArrivalDateTime
        FROM ScheduledOccurrences
    )
    INSERT INTO dbo.TripHistory (
        TripId,
        ServiceDate,
        OperatorId,
        RouteId,
        ScheduledDepartureDateTime,
        ScheduledArrivalDateTime,
        ActualDepartureDateTime,
        ActualArrivalDateTime,
        ActualTimesEstimated,
        PassengerVolume,
        TotalRevenue,
        SalesDataComplete,
        ArchivedBy
    )
    SELECT
        D.TripId,
        D.ServiceDate,
        D.OperatorId,
        D.RouteId,
        D.ScheduledDepartureDateTime,
        D.ScheduledArrivalDateTime,
        TODATETIMEOFFSET(D.ScheduledDepartureDateTime, @UtcOffsetMinutes),
        TODATETIMEOFFSET(D.ScheduledArrivalDateTime, @UtcOffsetMinutes),
        1,
        Sales.PassengerVolume,
        Sales.TotalRevenue,
        0,
        NULL
    FROM DueOccurrences D
    OUTER APPLY (
        SELECT COUNT(1) AS PassengerVolume, COALESCE(SUM(T.Price), 0) AS TotalRevenue
        FROM dbo.Ticket T
        WHERE T.RouteTripId = D.TripId
          AND T.TripServiceDate = D.ServiceDate
    ) Sales
    WHERE D.ScheduledArrivalDateTime <= @LocalNow
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.TripHistory H WITH (UPDLOCK, HOLDLOCK)
          WHERE H.TripId = D.TripId AND H.ServiceDate = D.ServiceDate
      );

    DECLARE @ArchivedCount INT = @@ROWCOUNT;
    EXEC sys.sp_releaseapplock
        @Resource = N'TripHistoryDailyArchive',
        @LockOwner = N'Session';

    SELECT @ArchivedCount AS ArchivedCount;
END
GO