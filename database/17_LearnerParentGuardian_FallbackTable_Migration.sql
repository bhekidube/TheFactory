SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
    Fallback persistence table for parent/guardian details.
    Useful when institution.Learner.ParentGuardianContact column is absent.
*/

IF OBJECT_ID('institution.LearnerParentGuardian', 'U') IS NULL
BEGIN
    CREATE TABLE institution.LearnerParentGuardian (
        LearnerId INT NOT NULL,
        ParentFirstName NVARCHAR(100) NULL,
        ParentSurname NVARCHAR(100) NULL,
        ParentPhoneNumber NVARCHAR(30) NULL,
        ParentEmailAddress NVARCHAR(255) NULL,
        RelationshipToLearner NVARCHAR(50) NULL,
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_institution_LearnerParentGuardian_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_institution_LearnerParentGuardian PRIMARY KEY (LearnerId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_institution_LearnerParentGuardian_Learner'
      AND parent_object_id = OBJECT_ID('institution.LearnerParentGuardian')
)
BEGIN
    ALTER TABLE institution.LearnerParentGuardian
    ADD CONSTRAINT FK_institution_LearnerParentGuardian_Learner
        FOREIGN KEY (LearnerId) REFERENCES institution.Learner (Id);
END
GO
