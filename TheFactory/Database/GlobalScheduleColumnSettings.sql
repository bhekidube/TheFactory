IF OBJECT_ID(N'dbo.GlobalScheduleColumnSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GlobalScheduleColumnSettings
    (
        SettingId tinyint NOT NULL
            CONSTRAINT PK_GlobalScheduleColumnSettings PRIMARY KEY
            CONSTRAINT CK_GlobalScheduleColumnSettings_Singleton CHECK (SettingId = 1),
        BusVisible bit NOT NULL CONSTRAINT DF_GlobalScheduleColumnSettings_BusVisible DEFAULT (1),
        DepartureVisible bit NOT NULL CONSTRAINT DF_GlobalScheduleColumnSettings_DepartureVisible DEFAULT (1),
        PriceVisible bit NOT NULL CONSTRAINT DF_GlobalScheduleColumnSettings_PriceVisible DEFAULT (1),
        ActionVisible bit NOT NULL CONSTRAINT DF_GlobalScheduleColumnSettings_ActionVisible DEFAULT (1),
        UpdatedBy int NULL,
        UpdatedAtUtc datetime2(0) NOT NULL
            CONSTRAINT DF_GlobalScheduleColumnSettings_UpdatedAtUtc DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.GlobalScheduleColumnSettings
    WHERE SettingId = 1
)
BEGIN
    INSERT INTO dbo.GlobalScheduleColumnSettings (SettingId)
    VALUES (1);
END;
GO
