IF SCHEMA_ID('institution') IS NULL
BEGIN
    EXEC('CREATE SCHEMA institution');
END;

IF OBJECT_ID('institution.ClassSubject', 'U') IS NULL
BEGIN
    CREATE TABLE institution.ClassSubject (
        ClassId INT NOT NULL,
        SubjectId INT NOT NULL,
        CONSTRAINT PK_institution_ClassSubject PRIMARY KEY (ClassId, SubjectId),
        CONSTRAINT FK_institution_ClassSubject_Class FOREIGN KEY (ClassId) REFERENCES institution.Class (Id),
        CONSTRAINT FK_institution_ClassSubject_Subject FOREIGN KEY (SubjectId) REFERENCES institution.Subject (Id)
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.ClassSubject')
      AND name = 'IX_institution_ClassSubject_SubjectId'
)
BEGIN
    CREATE INDEX IX_institution_ClassSubject_SubjectId
        ON institution.ClassSubject (SubjectId);
END;
