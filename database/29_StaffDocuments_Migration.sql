BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID('institution.StaffDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE institution.StaffDocuments (
        DocumentId INT IDENTITY(1,1) NOT NULL,
        StaffId INT NOT NULL,
        DocumentName NVARCHAR(255) NOT NULL,
        DocumentType NVARCHAR(100) NOT NULL,
        Filepath NVARCHAR(500) NOT NULL,
        UploadedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_institution_StaffDocuments_UploadedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_institution_StaffDocuments PRIMARY KEY (DocumentId),
        CONSTRAINT FK_institution_StaffDocuments_Staff FOREIGN KEY (StaffId)
            REFERENCES institution.Staff (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_institution_StaffDocuments_StaffId
        ON institution.StaffDocuments (StaffId, UploadedAt DESC);
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
