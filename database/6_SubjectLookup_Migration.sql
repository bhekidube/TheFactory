BEGIN TRY
BEGIN TRANSACTION;

-- 1) Create the subject lookup table if it does not exist.
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

-- 2) Add the new foreign-key column to Mark for the safe migration path.
IF COL_LENGTH('institution.Mark', 'SubjectId') IS NULL
BEGIN
    ALTER TABLE institution.Mark ADD SubjectId INT NULL;
END;

-- 3) Ensure the subject table has the required indexes.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('institution.Subject') AND name = 'IX_institution_Subject_TenantId_IsActive')
BEGIN
    CREATE INDEX IX_institution_Subject_TenantId_IsActive
        ON institution.Subject (TenantId, IsActive);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('institution.Subject') AND name = 'IX_institution_Subject_TenantId_Name')
BEGIN
    CREATE INDEX IX_institution_Subject_TenantId_Name
        ON institution.Subject (TenantId, Name);
END;

-- 4) Seed the default subject catalog for every tenant without creating duplicates.
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

-- 5) Backfill the subject master table using the existing Mark.Subject values.
--    This preserves the original tenant boundaries and correctly collapses duplicates by TenantId + Name.
;WITH DistinctSubjects AS (
    SELECT DISTINCT
        m.TenantId,
        CASE
            WHEN LTRIM(RTRIM(CAST(m.Subject AS NVARCHAR(200)))) = ''
                THEN 'Unspecified'
            ELSE LTRIM(RTRIM(CAST(m.Subject AS NVARCHAR(200))))
        END AS SubjectName
    FROM institution.Mark m
    WHERE m.SubjectId IS NULL
)
, NewSubjects AS (
    SELECT ds.TenantId,
           ds.SubjectName,
           ROW_NUMBER() OVER (ORDER BY ds.TenantId, ds.SubjectName) AS SequenceNo
    FROM DistinctSubjects ds
    WHERE NOT EXISTS (
        SELECT 1
        FROM institution.Subject s
        WHERE s.TenantId = ds.TenantId
          AND s.Name = ds.SubjectName
    )
)
INSERT INTO institution.Subject (Id, TenantId, Name, Code, IsActive)
SELECT
    ((SELECT ISNULL(MAX(Id), 0) FROM institution.Subject) + ns.SequenceNo),
    ns.TenantId,
    ns.SubjectName,
    NULL,
    1
FROM NewSubjects ns;

-- 6) Migrate every existing Mark row to the corresponding Subject row.
--    Names are matched within the same tenant only so subjects from different tenants do not merge.
UPDATE m
SET SubjectId = s.Id
FROM institution.Mark AS m
INNER JOIN institution.Subject AS s
    ON s.TenantId = m.TenantId
   AND s.Name = CASE
                    WHEN LTRIM(RTRIM(CAST(m.Subject AS NVARCHAR(200)))) = ''
                        THEN 'Unspecified'
                    ELSE LTRIM(RTRIM(CAST(m.Subject AS NVARCHAR(200))))
                END
WHERE m.SubjectId IS NULL;

-- 7) Safety guard: fail before destructive removal if any rows remain unmigrated.
IF EXISTS (
    SELECT 1
    FROM institution.Mark
    WHERE SubjectId IS NULL
)
BEGIN
    RAISERROR('Subject migration incomplete: one or more Mark rows do not have a valid SubjectId.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

-- 8) Add the foreign key and required indexes to the final schema.
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_institution_Mark_Subject')
BEGIN
    ALTER TABLE institution.Mark WITH CHECK
        ADD CONSTRAINT FK_institution_Mark_Subject
        FOREIGN KEY (SubjectId) REFERENCES institution.Subject (Id);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('institution.Mark') AND name = 'IX_institution_Mark_SubjectId')
BEGIN
    CREATE INDEX IX_institution_Mark_SubjectId
        ON institution.Mark (SubjectId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('institution.Mark') AND name = 'IX_institution_Mark_TenantId_LearnerId')
BEGIN
    CREATE INDEX IX_institution_Mark_TenantId_LearnerId
        ON institution.Mark (TenantId, LearnerId);
END;

-- 9) Enforce the final schema and remove the legacy column only after full migration success.
ALTER TABLE institution.Mark ALTER COLUMN SubjectId INT NOT NULL;

IF COL_LENGTH('institution.Mark', 'Subject') IS NOT NULL
BEGIN
    ALTER TABLE institution.Mark DROP COLUMN Subject;
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
