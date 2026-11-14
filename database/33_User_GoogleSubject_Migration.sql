IF COL_LENGTH('dbo.User', 'GoogleSubject') IS NULL
BEGIN
    ALTER TABLE dbo.[User] ADD GoogleSubject NVARCHAR(255) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_User_GoogleSubject'
      AND object_id = OBJECT_ID('dbo.User')
)
BEGIN
    CREATE UNIQUE INDEX UX_User_GoogleSubject
    ON dbo.[User](GoogleSubject)
    WHERE GoogleSubject IS NOT NULL;
END
GO