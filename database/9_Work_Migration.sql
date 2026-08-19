IF SCHEMA_ID('institution') IS NULL
BEGIN
    EXEC('CREATE SCHEMA institution');
END;

IF OBJECT_ID('institution.Work', 'U') IS NULL
BEGIN
    CREATE TABLE institution.Work (
        Id INT NOT NULL,
        SchoolId INT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        WorkType NVARCHAR(50) NOT NULL,
        ClassId INT NOT NULL,
        DueDate DATE NULL,
        MaxScore INT NULL,
        IsArchived BIT NOT NULL CONSTRAINT DF_institution_Work_IsArchived DEFAULT (0),
        CONSTRAINT PK_institution_Work PRIMARY KEY (Id),
        CONSTRAINT FK_institution_Work_Class FOREIGN KEY (ClassId) REFERENCES institution.Class (Id)
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Work')
      AND name = 'IX_institution_Work_SchoolId_IsArchived'
)
BEGIN
    CREATE INDEX IX_institution_Work_SchoolId_IsArchived
        ON institution.Work (SchoolId, IsArchived, DueDate);
END;
