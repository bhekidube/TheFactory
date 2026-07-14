
select * from Operator

SELECT * FROM [Location] 

select * from Route

DECLARE @RC int
DECLARE @OperatorId int =1
DECLARE @FromId int = 1
DECLARE @ToId int = 4 
DECLARE @Date date = GETDATE()
DECLARE @DepartureTime time(7) = '08:30:00'
DECLARE @ArrivalTime time(7) = '20:30:00'
DECLARE @Price decimal(18,2) = 700.00
DECLARE @CreatedBy INT = 1
DECLARE @CreatedDate datetime = GETDATE()
DECLARE @UpdatedBy INT  
DECLARE @UpdatedDate datetime 

-- TODO: Set parameter values here.

EXECUTE @RC = [dbo].[InsertRoute] 
   @OperatorId
  ,@FromId
  ,@ToId
  ,@Date
  ,@DepartureTime
  ,@ArrivalTime
  ,@Price
  ,@CreatedBy
  ,@CreatedDate
  ,@UpdatedBy
  ,@UpdatedDate
GO