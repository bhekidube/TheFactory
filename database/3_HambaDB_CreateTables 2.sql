SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Create OperatorUser table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'OperatorUser')
BEGIN
    CREATE TABLE [dbo].[OperatorUser](
        [OperatorUserId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [OperatorId] INT NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    ) ON [PRIMARY]
END
GO

-- Add primary key if it does not exist
IF NOT EXISTS (
    SELECT * FROM sys.key_constraints kc
    JOIN sys.tables t ON kc.parent_object_id = t.object_id
    WHERE kc.[type] = 'PK' AND t.[name] = 'OperatorUser'
)
BEGIN
    ALTER TABLE [dbo].[OperatorUser] ADD CONSTRAINT [PK_OperatorUser] PRIMARY KEY CLUSTERED 
    (
        [OperatorUserId] ASC
    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END
GO

-- Add unique constraint to prevent duplicate UserId/OperatorId pairs
IF NOT EXISTS (
    SELECT * FROM sys.indexes WHERE name = 'UQ_OperatorUser_UserId_OperatorId'
)
BEGIN
    ALTER TABLE [dbo].[OperatorUser] ADD CONSTRAINT [UQ_OperatorUser_UserId_OperatorId] UNIQUE ([UserId], [OperatorId])
END
GO

-- Add foreign key constraint for UserId if it does not exist
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys fk
    JOIN sys.tables t ON fk.parent_object_id = t.object_id
    WHERE fk.[name] = 'FK_UserId_OperatorUser' AND t.[name] = 'OperatorUser'
)
BEGIN
    ALTER TABLE [dbo].[OperatorUser]  WITH CHECK ADD  CONSTRAINT [FK_UserId_OperatorUser] FOREIGN KEY([UserId])
    REFERENCES [dbo].[User] ([UserId])
END
GO

-- Add foreign key constraint for OperatorId if it does not exist
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys fk
    JOIN sys.tables t ON fk.parent_object_id = t.object_id
    WHERE fk.[name] = 'FK_OperatorId_OperatorUser' AND t.[name] = 'OperatorUser'
)
BEGIN
    ALTER TABLE [dbo].[OperatorUser]  WITH CHECK ADD  CONSTRAINT [FK_OperatorId_OperatorUser] FOREIGN KEY([OperatorId])
    REFERENCES [dbo].[Operator] ([OperatorId])
END
GO

-- Add index on UserId for performance if not exists
IF NOT EXISTS (
    SELECT * FROM sys.indexes idx
    JOIN sys.tables t ON idx.object_id = t.object_id
    WHERE idx.[name] = 'IX_OperatorUser_UserId' AND t.[name] = 'OperatorUser'
)
BEGIN
    CREATE INDEX [IX_OperatorUser_UserId] ON [dbo].[OperatorUser]([UserId])
END
GO

-- Add index on OperatorId for performance if not exists
IF NOT EXISTS (
    SELECT * FROM sys.indexes idx
    JOIN sys.tables t ON idx.object_id = t.object_id
    WHERE idx.[name] = 'IX_OperatorUser_OperatorId' AND t.[name] = 'OperatorUser'
)
BEGIN
    CREATE INDEX [IX_OperatorUser_OperatorId] ON [dbo].[OperatorUser]([OperatorId])
END
GO

-- Create OperatorUserRole table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'OperatorUserRole')
BEGIN
    CREATE TABLE [dbo].[OperatorUserRole] (
        [OperatorUserRoleId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserRoleId] INT NOT NULL,
        [OperatorUserId] INT NOT NULL,
        [AssignedDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

-- Add foreign key constraint for UserRoleId if it does not exist
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys fk
    JOIN sys.tables t ON fk.parent_object_id = t.object_id
    WHERE fk.[name] = 'FK_OperatorUserRole_UserRoleId' AND t.[name] = 'OperatorUserRole'
)
BEGIN
    ALTER TABLE [dbo].[OperatorUserRole] WITH CHECK ADD CONSTRAINT [FK_OperatorUserRole_UserRoleId] FOREIGN KEY([UserRoleId])
    REFERENCES [dbo].[UserRole] ([UserRoleId]);
END
GO

-- Clean up orphaned OperatorUserRole rows BEFORE adding the FK constraint
DELETE FROM [dbo].[OperatorUserRole]
WHERE OperatorUserId NOT IN (SELECT OperatorUserId FROM [dbo].[OperatorUser]);
GO

-- Now add the foreign key constraint for OperatorUserId
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys fk
    JOIN sys.tables t ON fk.parent_object_id = t.object_id
    WHERE fk.[name] = 'FK_OperatorUserRole_OperatorUserId' AND t.[name] = 'OperatorUserRole'
)
BEGIN
    ALTER TABLE [dbo].[OperatorUserRole] WITH CHECK ADD CONSTRAINT [FK_OperatorUserRole_OperatorUserId] FOREIGN KEY([OperatorUserId])
    REFERENCES [dbo].[OperatorUser] ([OperatorUserId]);
END
GO

-- Add index on UserRoleId for performance if not exists
IF NOT EXISTS (
    SELECT * FROM sys.indexes idx
    JOIN sys.tables t ON idx.object_id = t.object_id
    WHERE idx.[name] = 'IX_OperatorUserRole_UserRoleId' AND t.[name] = 'OperatorUserRole'
)
BEGIN
    CREATE INDEX [IX_OperatorUserRole_UserRoleId] ON [dbo].[OperatorUserRole]([UserRoleId])
END
GO

-- Add index on OperatorUserId for performance if not exists
IF NOT EXISTS (
    SELECT * FROM sys.indexes idx
    JOIN sys.tables t ON idx.object_id = t.object_id
    WHERE idx.[name] = 'IX_OperatorUserRole_OperatorUserId' AND t.[name] = 'OperatorUserRole'
)
BEGIN
    CREATE INDEX [IX_OperatorUserRole_OperatorUserId] ON [dbo].[OperatorUserRole]([OperatorUserId])
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Create SystemUserRole table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SystemUserRole')
BEGIN
    CREATE TABLE [dbo].[SystemUserRole](
        [SystemUserRoleId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [UserRoleId] INT NOT NULL,
        [AssignedDate] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    ) ON [PRIMARY]
END
GO

-- Add primary key if it does not exist
IF NOT EXISTS (
    SELECT * FROM sys.key_constraints kc
    JOIN sys.tables t ON kc.parent_object_id = t.object_id
    WHERE kc.[type] = 'PK' AND t.[name] = 'SystemUserRole'
)
BEGIN
    ALTER TABLE [dbo].[SystemUserRole] ADD CONSTRAINT [PK_SystemUserRole] PRIMARY KEY CLUSTERED 
    (
        [SystemUserRoleId] ASC
    )WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END
GO

-- Add foreign key constraint for UserId if it does not exist
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys fk
    JOIN sys.tables t ON fk.parent_object_id = t.object_id
    WHERE fk.[name] = 'FK_SystemUserRole_UserId' AND t.[name] = 'SystemUserRole'
)
BEGIN
    ALTER TABLE [dbo].[SystemUserRole]  WITH CHECK ADD  CONSTRAINT [FK_SystemUserRole_UserId] FOREIGN KEY([UserId])
    REFERENCES [dbo].[User] ([UserId])
END
GO

-- Add foreign key constraint for UserRoleId if it does not exist
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys fk
    JOIN sys.tables t ON fk.parent_object_id = t.object_id
    WHERE fk.[name] = 'FK_SystemUserRole_UserRoleId' AND t.[name] = 'SystemUserRole'
)
BEGIN
    ALTER TABLE [dbo].[SystemUserRole]  WITH CHECK ADD  CONSTRAINT [FK_SystemUserRole_UserRoleId] FOREIGN KEY([UserRoleId])
    REFERENCES [dbo].[UserRole] ([UserRoleId])
END
GO

-- Add index on UserId for performance if not exists
IF NOT EXISTS (
    SELECT * FROM sys.indexes idx
    JOIN sys.tables t ON idx.object_id = t.object_id
    WHERE idx.[name] = 'IX_SystemUserRole_UserId' AND t.[name] = 'SystemUserRole'
)
BEGIN
    CREATE INDEX [IX_SystemUserRole_UserId] ON [dbo].[SystemUserRole]([UserId])
END
GO

-- Add index on UserRoleId for performance if not exists
IF NOT EXISTS (
    SELECT * FROM sys.indexes idx
    JOIN sys.tables t ON idx.object_id = t.object_id
    WHERE idx.[name] = 'IX_SystemUserRole_UserRoleId' AND t.[name] = 'SystemUserRole'
)
BEGIN
    CREATE INDEX [IX_SystemUserRole_UserRoleId] ON [dbo].[SystemUserRole]([UserRoleId])
END
GO
