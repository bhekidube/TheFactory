IF OBJECT_ID('institution.Addresses', 'U') IS NULL
    THROW 50001, 'Run the shared addresses migration before creating the all-users directory view.', 1;
IF OBJECT_ID('institution.Staff', 'U') IS NULL
    THROW 50002, 'The institution.Staff table is required for the all-users directory view.', 1;
IF OBJECT_ID('institution.Learner', 'U') IS NULL
    THROW 50003, 'The institution.Learner table is required for the all-users directory view.', 1;
IF OBJECT_ID('institution.LearnerParentGuardian', 'U') IS NULL
    THROW 50004, 'Run the learner parent/guardian fallback migration before creating the all-users directory view.', 1;
IF COL_LENGTH('dbo.User', 'AddressId') IS NULL
   OR COL_LENGTH('institution.Staff', 'AddressId') IS NULL
   OR COL_LENGTH('institution.Learner', 'AddressId') IS NULL
   OR COL_LENGTH('institution.LearnerParentGuardian', 'AddressId') IS NULL
    THROW 50005, 'Run the shared addresses migration before creating the all-users directory view.', 1;

EXEC(N'
CREATE OR ALTER VIEW institution.vw_AllUsers
AS
    SELECT
        u.UserId AS SourceId,
        CAST(N''User'' AS NVARCHAR(30)) AS SourceType,
        CAST(u.Name AS NVARCHAR(300)) AS Name,
        CAST(ISNULL(u.Email, N'''') AS NVARCHAR(255)) AS Email,
        CAST(ISNULL(ur.Name, N''User'') AS NVARCHAR(50)) AS UserType,
        u.AddressId
    FROM dbo.[User] AS u
    LEFT JOIN dbo.UserRole AS ur ON ur.UserRoleId = u.UserRoleId

    UNION ALL

    SELECT
        s.Id AS SourceId,
        CAST(N''Staff'' AS NVARCHAR(30)) AS SourceType,
        CAST(LTRIM(RTRIM(CONCAT(ISNULL(s.FirstName, N''''), N'' '', ISNULL(s.Surname, N'''')))) AS NVARCHAR(300)) AS Name,
        CAST(ISNULL(s.Email, N'''') AS NVARCHAR(255)) AS Email,
        CAST(CONCAT(N''Staff - '', ISNULL(s.Role, N''Staff'')) AS NVARCHAR(50)) AS UserType,
        s.AddressId
    FROM institution.Staff AS s

    UNION ALL

    SELECT
        l.Id AS SourceId,
        CAST(N''Learner'' AS NVARCHAR(30)) AS SourceType,
        CAST(LTRIM(RTRIM(CONCAT(ISNULL(l.FirstName, N''''), N'' '', ISNULL(l.Surname, N'''')))) AS NVARCHAR(300)) AS Name,
        CAST(N'''' AS NVARCHAR(255)) AS Email,
        CAST(N''Learner'' AS NVARCHAR(50)) AS UserType,
        l.AddressId
    FROM institution.Learner AS l

    UNION ALL

    SELECT
        lpg.LearnerId AS SourceId,
        CAST(N''Parent/Guardian'' AS NVARCHAR(30)) AS SourceType,
        CAST(LTRIM(RTRIM(CONCAT(ISNULL(lpg.ParentFirstName, N''''), N'' '', ISNULL(lpg.ParentSurname, N'''')))) AS NVARCHAR(300)) AS Name,
        CAST(ISNULL(lpg.ParentEmailAddress, N'''') AS NVARCHAR(255)) AS Email,
        CAST(N''Parent/Guardian'' AS NVARCHAR(50)) AS UserType,
        lpg.AddressId
    FROM institution.LearnerParentGuardian AS lpg;
');
