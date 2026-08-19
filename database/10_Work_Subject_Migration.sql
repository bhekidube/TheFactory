IF COL_LENGTH('institution.Work', 'SubjectId') IS NULL
BEGIN
    ALTER TABLE institution.Work
    ADD SubjectId INT NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_institution_Work_Subject'
      AND parent_object_id = OBJECT_ID('institution.Work')
)
BEGIN
    ALTER TABLE institution.Work
    ADD CONSTRAINT FK_institution_Work_Subject
        FOREIGN KEY (SubjectId) REFERENCES institution.Subject (Id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Work')
      AND name = 'IX_institution_Work_SubjectId'
)
BEGIN
    CREATE INDEX IX_institution_Work_SubjectId
        ON institution.Work (SubjectId);
END;
