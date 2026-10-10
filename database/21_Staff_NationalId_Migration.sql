BEGIN TRY
BEGIN TRANSACTION;

IF COL_LENGTH('institution.Staff', 'national_id') IS NULL
BEGIN

    ALTER TABLE institution.Staff
        ADD national_id NVARCHAR(50) NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Staff')
      AND name = 'UX_institution_Staff_NationalId'
)
BEGIN
    EXEC sys.sp_executesql N'
        CREATE UNIQUE INDEX UX_institution_Staff_NationalId
            ON institution.Staff (national_id)
            WHERE national_id IS NOT NULL;';
END;

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
