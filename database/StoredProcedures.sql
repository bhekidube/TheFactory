-- Create a stored procedure to create a user
CREATE PROCEDURE CreateUser
    @UserRoleId INT,
    @Name NVARCHAR(100),
    @Email NVARCHAR(100),
    @CellPhoneNo NVARCHAR(30),
    @AlternateCellPhoneNo NVARCHAR(30)
AS
BEGIN
    INSERT INTO [User] (UserRoleId, Name, Email, CellPhoneNo, AlternateCellPhoneNo)
    VALUES (@UserRoleId, @Name, @Email, @CellPhoneNo, @AlternateCellPhoneNo);

    SELECT SCOPE_IDENTITY() AS NewUserId;
END;