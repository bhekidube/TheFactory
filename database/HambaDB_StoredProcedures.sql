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
GO

-- Stored procedure to insert a new Operator
CREATE PROCEDURE InsertOperator
    @Name NVARCHAR(100),
    @OperatorTypeId INT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Operator WHERE Name = @Name)
    BEGIN
        INSERT INTO Operator (Name, OperatorTypeId)
        VALUES (@Name, @OperatorTypeId);

        SELECT SCOPE_IDENTITY() AS NewOperatorId;
    END
    ELSE
    BEGIN
        SELECT OperatorId FROM Operator WHERE Name = @Name;
    END
END;
GO
-- Stored procedure to insert a new Route
ALTER PROCEDURE InsertRoute
    @OperatorId INT,
    @FromId INT,
    @ToId INT,
    @Date DATE,
    @DepartureTime TIME,
    @ArrivalTime TIME,
    @Price DECIMAL(18,2),
    @CreatedBy INT,
    @CreatedDate DATETIME,
    @UpdatedBy INT = NULL,
    @UpdatedDate DATETIME = NULL
AS
BEGIN
    INSERT INTO [Route] (
        OperatorId,
        FromId,
        ToId,
        [Date],
        DepartureTime,
        ArrivalTime,
        Price,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    )
    VALUES (
        @OperatorId,
        @FromId,
        @ToId,
        @Date,
        @DepartureTime,
        @ArrivalTime,
        @Price,
        @CreatedBy,
        @CreatedDate,
        @UpdatedBy,
        @UpdatedDate
    );

    SELECT SCOPE_IDENTITY() AS NewRouteId;
END;