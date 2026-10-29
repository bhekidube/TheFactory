BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID('institution.LearnerDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE institution.LearnerDocuments (
        DocumentId INT IDENTITY(1,1) NOT NULL,
        LearnerId INT NOT NULL,
        DocumentName NVARCHAR(255) NOT NULL,
        DocumentType NVARCHAR(100) NOT NULL,
        Filepath NVARCHAR(500) NOT NULL,
        UploadedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_institution_LearnerDocuments_UploadedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_institution_LearnerDocuments PRIMARY KEY (DocumentId),
        CONSTRAINT FK_institution_LearnerDocuments_Learner FOREIGN KEY (LearnerId)
            REFERENCES institution.Learner (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_institution_LearnerDocuments_LearnerId
        ON institution.LearnerDocuments (LearnerId, UploadedAt DESC);
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
