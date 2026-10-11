IF SCHEMA_ID('institution') IS NULL
BEGIN
    EXEC('CREATE SCHEMA institution');
END;

IF OBJECT_ID('institution.Addresses', 'U') IS NULL
BEGIN
    CREATE TABLE institution.Addresses (
        AddressId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_institution_Addresses PRIMARY KEY,
        AddressLine1 NVARCHAR(200) NULL,
        AddressLine2 NVARCHAR(200) NULL,
        City NVARCHAR(100) NULL,
        ProvinceState NVARCHAR(100) NULL,
        PostalCode NVARCHAR(20) NULL,
        Country NVARCHAR(100) NULL
    );
END;

IF COL_LENGTH('institution.Staff', 'AddressId') IS NULL
    ALTER TABLE institution.Staff ADD AddressId INT NULL;
IF COL_LENGTH('institution.Learner', 'AddressId') IS NULL
    ALTER TABLE institution.Learner ADD AddressId INT NULL;
IF OBJECT_ID('institution.LearnerParentGuardian', 'U') IS NOT NULL
   AND COL_LENGTH('institution.LearnerParentGuardian', 'AddressId') IS NULL
    ALTER TABLE institution.LearnerParentGuardian ADD AddressId INT NULL;
IF COL_LENGTH('dbo.User', 'AddressId') IS NULL
    ALTER TABLE dbo.[User] ADD AddressId INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_institution_Staff_Address')
    EXEC sys.sp_executesql N'ALTER TABLE institution.Staff ADD CONSTRAINT FK_institution_Staff_Address FOREIGN KEY (AddressId) REFERENCES institution.Addresses (AddressId);';
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_institution_Learner_Address')
    EXEC sys.sp_executesql N'ALTER TABLE institution.Learner ADD CONSTRAINT FK_institution_Learner_Address FOREIGN KEY (AddressId) REFERENCES institution.Addresses (AddressId);';
IF OBJECT_ID('institution.LearnerParentGuardian', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_institution_LearnerParentGuardian_Address')
    EXEC sys.sp_executesql N'ALTER TABLE institution.LearnerParentGuardian ADD CONSTRAINT FK_institution_LearnerParentGuardian_Address FOREIGN KEY (AddressId) REFERENCES institution.Addresses (AddressId);';
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_User_Address')
    EXEC sys.sp_executesql N'ALTER TABLE dbo.[User] ADD CONSTRAINT FK_User_Address FOREIGN KEY (AddressId) REFERENCES institution.Addresses (AddressId);';
