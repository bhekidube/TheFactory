CREATE TABLE Ticket (
    TicketId INT IDENTITY(1,1) PRIMARY KEY,
    RouteId INT NOT NULL,
    UserId INT NOT NULL,
    SeatNumber NVARCHAR(20) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    PurchaseDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(50) NOT NULL,
    FOREIGN KEY (RouteId) REFERENCES Route(RouteId),
    FOREIGN KEY (UserId) REFERENCES [User](UserId)
);

GO
-- Stored procedure to create a Ticket
CREATE PROCEDURE CreateTicket
    @RouteId INT,
    @UserId INT,
    @SeatNumber NVARCHAR(20),
    @Price DECIMAL(18,2),
    @Status NVARCHAR(50)
AS
BEGIN
    INSERT INTO Ticket (
        RouteId,
        UserId,
        SeatNumber,
        Price,
        PurchaseDate,
        Status
    )
    VALUES (
        @RouteId,
        @UserId,
        @SeatNumber,
        @Price,
        GETDATE(),
        @Status
    );

    SELECT SCOPE_IDENTITY() AS NewTicketId;
END;