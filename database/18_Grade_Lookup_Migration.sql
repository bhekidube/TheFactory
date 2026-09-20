BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID('institution.Grade', 'U') IS NULL
BEGIN
    CREATE TABLE institution.Grade (
        Id INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        SortOrder INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_institution_Grade_IsActive DEFAULT (1),
        CONSTRAINT PK_institution_Grade PRIMARY KEY (Id),
        CONSTRAINT UQ_institution_Grade_Name UNIQUE (Name)
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Grade')
      AND name = 'IX_institution_Grade_IsActive_SortOrder'
)
BEGIN
    CREATE INDEX IX_institution_Grade_IsActive_SortOrder
        ON institution.Grade (IsActive, SortOrder, Name);
END;

;WITH DefaultGrades AS (
    SELECT 1 AS Id, N'Baby Class' AS Name, 1 AS SortOrder
    UNION ALL SELECT 2, N'Middle Class', 2
    UNION ALL SELECT 3, N'ECD A', 3
    UNION ALL SELECT 4, N'ECD B', 4
    UNION ALL SELECT 5, N'Grade 1', 6
    UNION ALL SELECT 6, N'Nursery', 5
    UNION ALL SELECT 7, N'Grade 2', 7
    UNION ALL SELECT 8, N'Grade 3', 8
    UNION ALL SELECT 9, N'Grade 4', 9
)
INSERT INTO institution.Grade (Id, Name, SortOrder, IsActive)
SELECT dg.Id, dg.Name, dg.SortOrder, 1
FROM DefaultGrades AS dg
WHERE NOT EXISTS (
    SELECT 1
    FROM institution.Grade AS g
    WHERE g.Name = dg.Name
);

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