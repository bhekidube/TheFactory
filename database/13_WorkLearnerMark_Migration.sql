IF SCHEMA_ID('institution') IS NULL
BEGIN
    EXEC('CREATE SCHEMA institution');
END;

IF OBJECT_ID('institution.WorkLearnerMark', 'U') IS NULL
BEGIN
    CREATE TABLE institution.WorkLearnerMark (
        Id INT NOT NULL,
        WorkId INT NOT NULL,
        LearnerId INT NOT NULL,
        SchoolId INT NOT NULL,
        MarkObtained INT NULL,
        Comment NVARCHAR(500) NULL,
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_institution_WorkLearnerMark_UpdatedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_institution_WorkLearnerMark PRIMARY KEY (Id)
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_institution_WorkLearnerMark_Work'
      AND parent_object_id = OBJECT_ID('institution.WorkLearnerMark')
)
BEGIN
    ALTER TABLE institution.WorkLearnerMark
    ADD CONSTRAINT FK_institution_WorkLearnerMark_Work
        FOREIGN KEY (WorkId) REFERENCES institution.Work (Id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_institution_WorkLearnerMark_Learner'
      AND parent_object_id = OBJECT_ID('institution.WorkLearnerMark')
)
BEGIN
    ALTER TABLE institution.WorkLearnerMark
    ADD CONSTRAINT FK_institution_WorkLearnerMark_Learner
        FOREIGN KEY (LearnerId) REFERENCES institution.Learner (Id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.WorkLearnerMark')
      AND name = 'UX_institution_WorkLearnerMark_WorkLearnerSchool'
)
BEGIN
    CREATE UNIQUE INDEX UX_institution_WorkLearnerMark_WorkLearnerSchool
        ON institution.WorkLearnerMark (WorkId, LearnerId, SchoolId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.WorkLearnerMark')
      AND name = 'IX_institution_WorkLearnerMark_School_Work'
)
BEGIN
    CREATE INDEX IX_institution_WorkLearnerMark_School_Work
        ON institution.WorkLearnerMark (SchoolId, WorkId);
END;

IF EXISTS (
    SELECT 1
    FROM institution.WorkLearnerMark
    WHERE MarkObtained IS NOT NULL
      AND MarkObtained < 0
)
BEGIN
    THROW 50003, 'WorkLearnerMark contains negative MarkObtained values.', 1;
END;
