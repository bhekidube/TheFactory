

DECLARE @RC int
DECLARE @UserRoleId int=1

SELECT @UserRoleId = UserRoleId FROM UserRole WHERE Name LIKE 'SystemAdmin'


DECLARE @Name nvarchar(100)= 'Code Advocate'
DECLARE @Email nvarchar(100)= 'bhekinkosidube@gmail.com'
DECLARE @CellPhoneNo nvarchar(30)='0785570422'
DECLARE @AlternateCellPhoneNo nvarchar(30)=''

-- TODO: Set parameter values here.
BEGIN TRAN
EXECUTE @RC = [dbo].[CreateUser] 
   @UserRoleId
  ,@Name
  ,@Email
  ,@CellPhoneNo
  ,@AlternateCellPhoneNo

--COMMIT
--ROLLBACK

GO

{
  "operatorId": 6,
  "fromId": 1,
  "toId": 8,
  "createdBy": 1
}


EXEC sp_who2;  -- Check active queries
