BEGIN TRY
BEGIN TRANSACTION;

/*
    institution.Class.TeacherId historically stored [User].UserId.
    Staff 19 migration must run first. This migration backfills school staff
    records from those users, converts TeacherId to Staff.Id, and then adds
    the Staff foreign key.
*/

IF OBJECT_ID('institution.Class', 'U') IS NULL
BEGIN
    RAISERROR('institution.Class does not exist.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

IF OBJECT_ID('institution.Staff', 'U') IS NULL
BEGIN
    RAISERROR('institution.Staff does not exist. Run 19_Staff_Migration.sql first.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

DECLARE @NextStaffId INT = ISNULL((SELECT MAX(Id) FROM institution.Staff), 0);

;WITH MissingUsers AS (
    SELECT DISTINCT
        c.SchoolId,
        c.TeacherId AS UserId,
        u.Name,
        u.Email,
        u.CellPhoneNo,
        ROW_NUMBER() OVER (ORDER BY c.SchoolId, c.TeacherId) AS SequenceNo
    FROM institution.Class AS c
    INNER JOIN [User] AS u ON u.UserId = c.TeacherId
    WHERE NOT EXISTS (
        SELECT 1
        FROM institution.Staff AS s
        WHERE s.SchoolId = c.SchoolId
          AND s.UserId = c.TeacherId
    )
)
INSERT INTO institution.Staff
    (Id, SchoolId, UserId, FirstName, Surname, Role, Email, Phone, Status, CreatedAt, UpdatedAt)
SELECT
    @NextStaffId + SequenceNo,
    SchoolId,
    UserId,
    CASE WHEN CHARINDEX(' ', LTRIM(RTRIM(Name))) > 0
         THEN LEFT(LTRIM(RTRIM(Name)), CHARINDEX(' ', LTRIM(RTRIM(Name))) - 1)
         ELSE LTRIM(RTRIM(Name)) END,
    CASE WHEN CHARINDEX(' ', LTRIM(RTRIM(Name))) > 0
         THEN LTRIM(SUBSTRING(LTRIM(RTRIM(Name)), CHARINDEX(' ', LTRIM(RTRIM(Name))) + 1, 200))
         ELSE '' END,
    'Teacher',
    ISNULL(Email, ''),
    ISNULL(CellPhoneNo, ''),
    'Active',
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM MissingUsers;

UPDATE c
SET TeacherId = s.Id
FROM institution.Class AS c
INNER JOIN institution.Staff AS s
    ON s.SchoolId = c.SchoolId
   AND s.UserId = c.TeacherId
WHERE c.TeacherId <> s.Id;

/*
    Some legacy classes may reference a deleted user or a user outside the
    school. Preserve those class rows with an explicit legacy Staff record so
    the new foreign key can still be enforced.
*/
IF EXISTS (
    SELECT 1
    FROM institution.Class AS c
    WHERE NOT EXISTS (
        SELECT 1
        FROM institution.Staff AS s
        WHERE s.Id = c.TeacherId
          AND s.SchoolId = c.SchoolId
    )
)
BEGIN
    CREATE TABLE #UnresolvedClassTeachers (
        SchoolId INT NOT NULL,
        LegacyTeacherId INT NOT NULL,
        StaffId INT NOT NULL,
        PRIMARY KEY (SchoolId, LegacyTeacherId)
    );

    DECLARE @LegacyStaffStartId INT = ISNULL((SELECT MAX(Id) FROM institution.Staff), 0);

    INSERT INTO #UnresolvedClassTeachers (SchoolId, LegacyTeacherId, StaffId)
    SELECT
        unresolved.SchoolId,
        unresolved.TeacherId,
        @LegacyStaffStartId + ROW_NUMBER() OVER (ORDER BY unresolved.SchoolId, unresolved.TeacherId)
    FROM (
        SELECT DISTINCT c.SchoolId, c.TeacherId
        FROM institution.Class AS c
        WHERE NOT EXISTS (
            SELECT 1
            FROM institution.Staff AS s
            WHERE s.Id = c.TeacherId
              AND s.SchoolId = c.SchoolId
        )
    ) AS unresolved;

    INSERT INTO institution.Staff
        (Id, SchoolId, UserId, FirstName, Surname, Role, Email, Phone, Status, CreatedAt, UpdatedAt)
    SELECT
        StaffId,
        SchoolId,
        NULL,
        'Legacy',
        CONCAT('Teacher ', LegacyTeacherId),
        'Teacher',
        CONCAT('legacy-teacher-', SchoolId, '-', LegacyTeacherId, '@invalid.local'),
        '',
        'Active',
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    FROM #UnresolvedClassTeachers;

    UPDATE c
    SET TeacherId = unresolved.StaffId
    FROM institution.Class AS c
    INNER JOIN #UnresolvedClassTeachers AS unresolved
        ON unresolved.SchoolId = c.SchoolId
       AND unresolved.LegacyTeacherId = c.TeacherId;
END;

IF EXISTS (
    SELECT 1
    FROM institution.Class AS c
    WHERE NOT EXISTS (
        SELECT 1
        FROM institution.Staff AS s
        WHERE s.Id = c.TeacherId
          AND s.SchoolId = c.SchoolId
    )
)
BEGIN
    RAISERROR('Class teacher migration incomplete: one or more classes do not reference a Staff record.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_institution_Class_Teacher_Staff'
      AND parent_object_id = OBJECT_ID('institution.Class')
)
BEGIN
    ALTER TABLE institution.Class WITH CHECK
        ADD CONSTRAINT FK_institution_Class_Teacher_Staff
        FOREIGN KEY (TeacherId) REFERENCES institution.Staff (Id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('institution.Class')
      AND name = 'IX_institution_Class_TeacherId'
)
BEGIN
    CREATE INDEX IX_institution_Class_TeacherId
        ON institution.Class (TeacherId);
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
