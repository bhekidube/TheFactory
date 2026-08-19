IF SCHEMA_ID('institution') IS NULL
BEGIN
    EXEC('CREATE SCHEMA institution');
END;

IF OBJECT_ID('institution.Class', 'U') IS NULL
BEGIN
    CREATE TABLE institution.Class (
        Id INT NOT NULL,
        SchoolId INT NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Grade NVARCHAR(50) NOT NULL,
        TeacherId INT NOT NULL,
        CONSTRAINT PK_institution_Class PRIMARY KEY (Id)
    );
END;

IF OBJECT_ID('institution.ClassEnrolment', 'U') IS NULL
BEGIN
    CREATE TABLE institution.ClassEnrolment (
        ClassId INT NOT NULL,
        LearnerId INT NOT NULL,
        CONSTRAINT PK_institution_ClassEnrolment PRIMARY KEY (ClassId, LearnerId),
        CONSTRAINT FK_institution_ClassEnrolment_Class FOREIGN KEY (ClassId) REFERENCES institution.Class (Id),
        CONSTRAINT FK_institution_ClassEnrolment_Learner FOREIGN KEY (LearnerId) REFERENCES institution.Learner (Id)
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Class')
      AND name = 'IX_institution_Class_SchoolId'
)
BEGIN
    CREATE INDEX IX_institution_Class_SchoolId
        ON institution.Class (SchoolId, Grade);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.ClassEnrolment')
      AND name = 'IX_institution_ClassEnrolment_LearnerId'
)
BEGIN
    CREATE INDEX IX_institution_ClassEnrolment_LearnerId
        ON institution.ClassEnrolment (LearnerId);
END;
