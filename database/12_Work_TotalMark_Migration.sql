IF SCHEMA_ID('institution') IS NULL
BEGIN
    EXEC('CREATE SCHEMA institution');
END;

IF COL_LENGTH('institution.Work', 'TotalMark') IS NULL
BEGIN
    ALTER TABLE institution.Work
    ADD TotalMark INT NULL;
END;

EXEC sp_executesql N'
UPDATE institution.Work
SET TotalMark = ISNULL(MaxScore, 100)
WHERE TotalMark IS NULL;';

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('institution.Work')
      AND name = 'TotalMark'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE institution.Work
    ALTER COLUMN TotalMark INT NOT NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID('institution.Work')
      AND c.name = 'TotalMark'
)
BEGIN
    ALTER TABLE institution.Work
    ADD CONSTRAINT DF_institution_Work_TotalMark DEFAULT (100) FOR TotalMark;
END;

DECLARE @NonPositiveTotalMarkCount INT = 0;
EXEC sp_executesql
    N'SELECT @Count = COUNT(1) FROM institution.Work WHERE TotalMark <= 0;',
    N'@Count INT OUTPUT',
    @Count = @NonPositiveTotalMarkCount OUTPUT;

IF (@NonPositiveTotalMarkCount > 0)
BEGIN
    THROW 50002, 'TotalMark migration failed. Rows with non-positive TotalMark detected.', 1;
END;
