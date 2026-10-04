BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.User', 'ResetToken') IS NULL
    BEGIN
        ALTER TABLE dbo.[User]
            ADD ResetToken NVARCHAR(256) NULL;
    END;

    IF COL_LENGTH('dbo.User', 'ResetTokenExpiry') IS NULL
    BEGIN
        ALTER TABLE dbo.[User]
            ADD ResetTokenExpiry DATETIME2(7) NULL;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
