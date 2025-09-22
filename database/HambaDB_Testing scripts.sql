

DECLARE @RC int
DECLARE @UserRoleId int=1

SELECT @UserRoleId = UserRoleId FROM UserRole WHERE Name LIKE 'SystemAdmin'


DECLARE @Name nvarchar(100)= 'Dev Advocate'
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