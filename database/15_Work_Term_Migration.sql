-- Adds required Term column to institution.Work and backfills existing records.

IF COL_LENGTH('institution.Work', 'Term') IS NULL
BEGIN
    ALTER TABLE institution.Work
    ADD Term NVARCHAR(20) NULL;
END;

EXEC sp_executesql N'
UPDATE institution.Work
SET Term = ''Term 1''
WHERE Term IS NULL OR LTRIM(RTRIM(Term)) = '''';
';

IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'institution'
      AND t.name = 'Work'
      AND c.name = 'Term'
      AND c.is_nullable = 1
)
BEGIN
    EXEC sp_executesql N'
    ALTER TABLE institution.Work
    ALTER COLUMN Term NVARCHAR(20) NOT NULL;
    ';
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints cc
    INNER JOIN sys.tables t ON t.object_id = cc.parent_object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'institution'
      AND t.name = 'Work'
      AND cc.name = 'CK_Work_Term_Allowed'
)
BEGIN
    EXEC sp_executesql N'
    ALTER TABLE institution.Work
    ADD CONSTRAINT CK_Work_Term_Allowed
        CHECK (Term IN (''Term 1'', ''Term 2'', ''Term 3'', ''Term 4''));
    ';
END;
