IF SCHEMA_ID('institution') IS NULL
BEGIN
    EXEC('CREATE SCHEMA institution');
END;

IF OBJECT_ID('institution.Tenant', 'U') IS NULL
BEGIN
    CREATE TABLE institution.Tenant (
        Id INT PRIMARY KEY,
        Name NVARCHAR(200),
        LogoUrl NVARCHAR(500)
    );
END;

IF OBJECT_ID('institution.Learner', 'U') IS NULL
BEGIN
    CREATE TABLE institution.Learner (
        Id INT PRIMARY KEY,
        TenantId INT,
        FirstName NVARCHAR(100),
        Surname NVARCHAR(100),
        Grade NVARCHAR(50)
    );
END;

IF OBJECT_ID('institution.Subject', 'U') IS NULL
BEGIN
    CREATE TABLE institution.Subject (
        Id INT NOT NULL,
        TenantId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Code NVARCHAR(50) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_institution_Subject_IsActive DEFAULT (1),
        CONSTRAINT PK_institution_Subject PRIMARY KEY (Id),
        CONSTRAINT UQ_institution_Subject_TenantId_Name UNIQUE (TenantId, Name)
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Subject')
      AND name = 'IX_institution_Subject_TenantId_IsActive'
)
BEGIN
    CREATE INDEX IX_institution_Subject_TenantId_IsActive
        ON institution.Subject (TenantId, IsActive);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Subject')
      AND name = 'IX_institution_Subject_TenantId_Name'
)
BEGIN
    CREATE INDEX IX_institution_Subject_TenantId_Name
        ON institution.Subject (TenantId, Name);
END;

;WITH DefaultSubjects AS (
    SELECT CAST('ENGLISH' AS NVARCHAR(200)) AS Name, CAST(NULL AS NVARCHAR(50)) AS Code
    UNION ALL SELECT 'NDEBELE', NULL
    UNION ALL SELECT 'MATHEMATICS', NULL
    UNION ALL SELECT 'AGRICULTURE', NULL
    UNION ALL SELECT 'SCIENCE & TECHNOLOGY', NULL
    UNION ALL SELECT 'SOCIAL SCIENCE', NULL
    UNION ALL SELECT 'PHYSICAL EDUCATION', NULL
    UNION ALL SELECT 'ARTS', NULL
), MissingSubjects AS (
    SELECT
        t.Id AS TenantId,
        ds.Name,
        ds.Code,
        ROW_NUMBER() OVER (ORDER BY t.Id, ds.Name) AS SequenceNo
    FROM institution.Tenant AS t
    CROSS JOIN DefaultSubjects AS ds
    WHERE NOT EXISTS (
        SELECT 1
        FROM institution.Subject AS s
        WHERE s.TenantId = t.Id
          AND s.Name = ds.Name
    )
)
INSERT INTO institution.Subject (Id, TenantId, Name, Code, IsActive)
SELECT
    (SELECT ISNULL(MAX(Id), 0) FROM institution.Subject) + ms.SequenceNo,
    ms.TenantId,
    ms.Name,
    ms.Code,
    1
FROM MissingSubjects AS ms;

IF OBJECT_ID('institution.Mark', 'U') IS NULL
BEGIN
    CREATE TABLE institution.Mark (
        Id INT NOT NULL,
        TenantId INT NOT NULL,
        LearnerId INT NOT NULL,
        SubjectId INT NOT NULL,
        Score INT NOT NULL,
        TeacherComments NVARCHAR(MAX) NULL,
        CONSTRAINT PK_institution_Mark PRIMARY KEY (Id),
        CONSTRAINT FK_institution_Mark_Subject FOREIGN KEY (SubjectId) REFERENCES institution.Subject (Id)
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Mark')
      AND name = 'IX_institution_Mark_TenantId_LearnerId'
)
BEGIN
    CREATE INDEX IX_institution_Mark_TenantId_LearnerId
        ON institution.Mark (TenantId, LearnerId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Mark')
      AND name = 'IX_institution_Mark_SubjectId'
)
BEGIN
    CREATE INDEX IX_institution_Mark_SubjectId
        ON institution.Mark (SubjectId);
END;