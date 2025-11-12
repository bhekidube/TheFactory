SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Create OperatorUser table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'OperatorUser')
BEGIN
    CREATE TABLE [dbo].[OperatorUser](
        [OperatorUserId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL
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

-- Add foreign key constraint if it does not exist
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

-- Create OperatorUserRole table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'OperatorUserRole')
BEGIN
    CREATE TABLE [dbo].[OperatorUserRole] (
        [OperatorUserRoleId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserRoleId] INT NOT NULL,
        [OperatorUserId] INT NOT NULL
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

-- Add foreign key constraint for OperatorUserId if it does not exist
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
