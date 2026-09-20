SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
    Adds optional ParentUserId linkage to reuse existing [User] records for guardians.
    Safe to run multiple times.
*/

IF COL_LENGTH('institution.Learner', 'ParentUserId') IS NULL
BEGIN
    ALTER TABLE institution.Learner
    ADD ParentUserId INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_institution_Learner_ParentUser'
      AND parent_object_id = OBJECT_ID('institution.Learner')
)
BEGIN
    ALTER TABLE institution.Learner
    ADD CONSTRAINT FK_institution_Learner_ParentUser
        FOREIGN KEY (ParentUserId) REFERENCES [dbo].[User](UserId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Learner')
      AND name = 'IX_institution_Learner_ParentUserId'
)
BEGIN
    CREATE INDEX IX_institution_Learner_ParentUserId
        ON institution.Learner (ParentUserId);
END
GO
