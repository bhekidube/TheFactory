IF SCHEMA_ID('institution') IS NULL
BEGIN
    EXEC('CREATE SCHEMA institution');
END;

IF OBJECT_ID('institution.WorkTypes', 'U') IS NULL
BEGIN
    CREATE TABLE institution.WorkTypes (
        Id INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        CONSTRAINT PK_institution_WorkTypes PRIMARY KEY (Id),
        CONSTRAINT UQ_institution_WorkTypes_Name UNIQUE (Name)
    );
END;

IF NOT EXISTS (SELECT 1 FROM institution.WorkTypes WHERE Id = 1)
BEGIN
    INSERT INTO institution.WorkTypes (Id, Name) VALUES (1, 'Homework');
END;

IF NOT EXISTS (SELECT 1 FROM institution.WorkTypes WHERE Id = 2)
BEGIN
    INSERT INTO institution.WorkTypes (Id, Name) VALUES (2, 'Classwork');
END;

IF NOT EXISTS (SELECT 1 FROM institution.WorkTypes WHERE Id = 3)
BEGIN
    INSERT INTO institution.WorkTypes (Id, Name) VALUES (3, 'Assignment');
END;

IF NOT EXISTS (SELECT 1 FROM institution.WorkTypes WHERE Id = 4)
BEGIN
    INSERT INTO institution.WorkTypes (Id, Name) VALUES (4, 'Project');
END;

IF NOT EXISTS (SELECT 1 FROM institution.WorkTypes WHERE Id = 5)
BEGIN
    INSERT INTO institution.WorkTypes (Id, Name) VALUES (5, 'Assessment / Test');
END;

IF COL_LENGTH('institution.Work', 'WorkTypeId') IS NULL
BEGIN
    ALTER TABLE institution.Work
    ADD WorkTypeId INT NULL;
END;

EXEC sp_executesql N'
UPDATE w
SET WorkTypeId = wt.Id
FROM institution.Work AS w
INNER JOIN institution.WorkTypes AS wt ON wt.Name = w.WorkType
WHERE w.WorkTypeId IS NULL;';

EXEC sp_executesql N'
UPDATE institution.Work
SET WorkTypeId = 5
WHERE WorkTypeId IS NULL
    AND WorkType IN (''Test'', ''Exam'', ''Assessment'', ''Assessment/Test'', ''Assessment / Test'');';

EXEC sp_executesql N'
UPDATE institution.Work
SET WorkTypeId = 2
WHERE WorkTypeId IS NULL
    AND WorkType = ''Exercise'';';

EXEC sp_executesql N'
UPDATE institution.Work
SET WorkTypeId = 3
WHERE WorkTypeId IS NULL;';

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID('institution.Work')
      AND c.name = 'WorkTypeId'
)
BEGIN
    ALTER TABLE institution.Work
    ADD CONSTRAINT DF_institution_Work_WorkTypeId DEFAULT (3) FOR WorkTypeId;
END;

DECLARE @NullWorkTypeCount INT = 0;
EXEC sp_executesql
    N'SELECT @NullCount = COUNT(1) FROM institution.Work WHERE WorkTypeId IS NULL;',
    N'@NullCount INT OUTPUT',
    @NullCount = @NullWorkTypeCount OUTPUT;

IF (@NullWorkTypeCount > 0)
BEGIN
    THROW 50001, 'WorkTypeId backfill failed. Some rows still have NULL WorkTypeId.', 1;
END;

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('institution.Work')
      AND name = 'WorkTypeId'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE institution.Work
    ALTER COLUMN WorkTypeId INT NOT NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_institution_Work_WorkTypes'
      AND parent_object_id = OBJECT_ID('institution.Work')
)
BEGIN
    ALTER TABLE institution.Work
    ADD CONSTRAINT FK_institution_Work_WorkTypes
        FOREIGN KEY (WorkTypeId) REFERENCES institution.WorkTypes (Id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Work')
      AND name = 'IX_institution_Work_WorkTypeId'
)
BEGIN
    CREATE INDEX IX_institution_Work_WorkTypeId
        ON institution.Work (WorkTypeId);
END;
