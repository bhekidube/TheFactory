BEGIN TRY
BEGIN TRANSACTION;

IF SCHEMA_ID('institution') IS NULL
BEGIN
    EXEC('CREATE SCHEMA institution');
END;

IF OBJECT_ID('institution.Staff', 'U') IS NULL
BEGIN
    CREATE TABLE institution.Staff (
        Id INT NOT NULL,
        SchoolId INT NOT NULL,
        UserId INT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        Surname NVARCHAR(100) NOT NULL,
        Role NVARCHAR(100) NOT NULL CONSTRAINT DF_institution_Staff_Role DEFAULT ('Staff'),
        Email NVARCHAR(255) NOT NULL,
        Phone NVARCHAR(30) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_institution_Staff_Status DEFAULT ('Active'),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_institution_Staff_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_institution_Staff_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_institution_Staff PRIMARY KEY (Id),
        CONSTRAINT FK_institution_Staff_School FOREIGN KEY (SchoolId) REFERENCES institution.Tenant (Id),
        CONSTRAINT FK_institution_Staff_User FOREIGN KEY (UserId) REFERENCES [User] (UserId),
        CONSTRAINT CK_institution_Staff_Status CHECK (Status IN ('Active', 'Archived'))
    );
END;

IF COL_LENGTH('institution.Staff', 'Role') IS NULL
BEGIN
    ALTER TABLE institution.Staff
        ADD Role NVARCHAR(100) NOT NULL CONSTRAINT DF_institution_Staff_Role DEFAULT ('Staff');
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Staff')
      AND name = 'UX_institution_Staff_SchoolId_Email'
)
BEGIN
    CREATE UNIQUE INDEX UX_institution_Staff_SchoolId_Email
        ON institution.Staff (SchoolId, Email);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Staff')
      AND name = 'IX_institution_Staff_SchoolId_Status'
)
BEGIN
    CREATE INDEX IX_institution_Staff_SchoolId_Status
        ON institution.Staff (SchoolId, Status, FirstName, Surname);
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