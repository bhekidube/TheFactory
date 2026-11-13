IF COL_LENGTH('dbo.Ticket', 'RouteTripId') IS NULL
BEGIN
    ALTER TABLE dbo.Ticket ADD RouteTripId INT NULL;
END
GO

IF COL_LENGTH('dbo.Ticket', 'TripServiceDate') IS NULL
BEGIN
    ALTER TABLE dbo.Ticket ADD TripServiceDate DATE NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Ticket_RouteTrip')
BEGIN
    ALTER TABLE dbo.Ticket WITH CHECK
    ADD CONSTRAINT FK_Ticket_RouteTrip FOREIGN KEY (RouteTripId) REFERENCES dbo.RouteTrip(TripId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Ticket_TripServiceReference')
BEGIN
    ALTER TABLE dbo.Ticket WITH CHECK
    ADD CONSTRAINT CK_Ticket_TripServiceReference
    CHECK ((RouteTripId IS NULL AND TripServiceDate IS NULL) OR
           (RouteTripId IS NOT NULL AND TripServiceDate IS NOT NULL));
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ticket_RouteTripId_TripServiceDate')
BEGIN
    CREATE INDEX IX_Ticket_RouteTripId_TripServiceDate
    ON dbo.Ticket(RouteTripId, TripServiceDate)
    INCLUDE (Price, Status);
END
GO

IF OBJECT_ID('dbo.TripHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TripHistory (
        TripHistoryId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TripId INT NOT NULL,
        ServiceDate DATE NOT NULL,
        OperatorId INT NOT NULL,
        RouteId INT NOT NULL,
        ScheduledDepartureDateTime DATETIME2 NOT NULL,
        ScheduledArrivalDateTime DATETIME2 NOT NULL,
        ActualDepartureDateTime DATETIMEOFFSET NOT NULL,
        ActualArrivalDateTime DATETIMEOFFSET NOT NULL,
        PassengerVolume INT NOT NULL,
        TotalRevenue DECIMAL(18,2) NOT NULL,
        SalesDataComplete BIT NOT NULL,
        ArchivedBy INT NOT NULL,
        ArchivedAt DATETIME2 NOT NULL CONSTRAINT DF_TripHistory_ArchivedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_TripHistory_TripId_ServiceDate UNIQUE (TripId, ServiceDate),
        CONSTRAINT FK_TripHistory_RouteTrip FOREIGN KEY (TripId) REFERENCES dbo.RouteTrip(TripId),
        CONSTRAINT FK_TripHistory_Operator FOREIGN KEY (OperatorId) REFERENCES dbo.Operator(OperatorId),
        CONSTRAINT FK_TripHistory_Route FOREIGN KEY (RouteId) REFERENCES dbo.Route(RouteId),
        CONSTRAINT FK_TripHistory_ArchivedBy FOREIGN KEY (ArchivedBy) REFERENCES dbo.[User](UserId),
        CONSTRAINT CK_TripHistory_ActualTimes CHECK (ActualArrivalDateTime > ActualDepartureDateTime),
        CONSTRAINT CK_TripHistory_Sales CHECK (PassengerVolume >= 0 AND TotalRevenue >= 0)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TripHistory_Operator_ServiceDate')
BEGIN
    CREATE INDEX IX_TripHistory_Operator_ServiceDate
    ON dbo.TripHistory(OperatorId, ServiceDate)
    INCLUDE (RouteId, PassengerVolume, TotalRevenue, SalesDataComplete);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TripHistory_Route_ServiceDate')
BEGIN
    CREATE INDEX IX_TripHistory_Route_ServiceDate
    ON dbo.TripHistory(RouteId, ServiceDate)
    INCLUDE (OperatorId, PassengerVolume, TotalRevenue, SalesDataComplete);
END
GO