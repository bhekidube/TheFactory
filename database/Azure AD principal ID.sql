-- List all Azure AD users and groups in this database
SELECT
    dp.name AS DatabaseUserName,
    dp.type_desc AS PrincipalType,
    dp.authentication_type_desc AS AuthType,
    dp.create_date,
    dp.modify_date,
    dp.authentication_type
FROM sys.database_principals dp
ORDER BY dp.name;

SELECT 
    dp.name AS database_user, 
    rolep.name AS role_name
FROM sys.database_principals dp
JOIN sys.database_role_members drm 
    ON dp.principal_id = drm.member_principal_id
JOIN sys.database_principals rolep 
    ON drm.role_principal_id = rolep.principal_id
WHERE dp.authentication_type = 4;


--f the SID doesn’t match your App Service’s managed identity, you need to map them.
SELECT name, type_desc, sid
FROM sys.database_principals
WHERE name = 'HambaApp';


--1

--AzureLinuxAppService -- 61230061-0ba5-45f8-a8b6-37ab66a0ab2c

-- Create the external user for the managed identity
CREATE USER [AzureLinuxAppService] FROM EXTERNAL PROVIDER;

-- Grant minimal permissions
ALTER ROLE db_datareader ADD MEMBER [AzureLinuxAppService];
ALTER ROLE db_datawriter ADD MEMBER [AzureLinuxAppService];

--2
/*
DROP USER [AzureAngularAppService];
DROP USER [HambaApp];

SELECT s.name AS schema_name,
       u.name AS schema_owner
FROM sys.schemas s
JOIN sys.database_principals u
    ON s.principal_id = u.principal_id
WHERE u.name IN ('AzureAngularAppService', 'HambaApp');

DECLARE @userName SYSNAME;

DECLARE cur CURSOR FOR
SELECT name
FROM sys.database_principals
WHERE name IN ('AzureAngularAppService', 'HambaApp');

OPEN cur;
FETCH NEXT FROM cur INTO @userName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.schemas s
        JOIN sys.database_principals u
            ON s.principal_id = u.principal_id
        WHERE u.name = @userName
    )
    BEGIN
        DECLARE @schemaName SYSNAME;
        SELECT @schemaName = s.name
        FROM sys.schemas s
        JOIN sys.database_principals u
            ON s.principal_id = u.principal_id
        WHERE u.name = @userName;

        EXEC('ALTER AUTHORIZATION ON SCHEMA::[' + @schemaName + '] TO dbo;');
    END

    EXEC('DROP USER [' + @userName + '];');

    FETCH NEXT FROM cur INTO @userName;
END

CLOSE cur;
DEALLOCATE cur;


*/

SELECT name, type_desc, authentication_type_desc
FROM sys.database_principals
WHERE name = 'AzureLinuxAppService';

--GRANT EXECUTE ON OBJECT::dbo.InsertRoute TO [AzureLinuxAppService];




SELECT GETDATE()