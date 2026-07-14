
--IF OBJECT_ID('Ticket', 'U') IS NOT NULL
--    ALTER TABLE Ticket DROP CONSTRAINT IF EXISTS FK__Ticket__RouteId__5E3FF0B0;

SELECT 
    fk.name AS FK_Name,
    t.name AS Table_Name,
    c.name AS Column_Name
FROM sys.foreign_keys fk
JOIN sys.tables t ON fk.parent_object_id = t.object_id
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
WHERE fk.referenced_object_id = OBJECT_ID('Route');

SELECT 
    session_id, 
    memory_usage, 
    status, 
    login_name, 
    host_name, 
    program_name
FROM sys.dm_exec_sessions
ORDER BY memory_usage DESC;