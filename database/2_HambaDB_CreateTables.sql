CREATE TABLE Country (
    CountryId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

CREATE TABLE Region (
    RegionId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    CountryId INT NOT NULL,
    FOREIGN KEY (CountryId) REFERENCES Country(CountryId)
);

CREATE TABLE Town (
    TownId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    RegionId INT NOT NULL,
    FOREIGN KEY (RegionId) REFERENCES Region(RegionId)
);

CREATE TABLE LocationType (
    LocationTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);

CREATE TABLE Location (
    LocationId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    TownId INT NOT NULL,
    RegionId INT NOT NULL,
    CountryId INT NOT NULL,
    LocationTypeId INT NOT NULL,
    FOREIGN KEY (TownId) REFERENCES Town(TownId),
    FOREIGN KEY (RegionId) REFERENCES Region(RegionId),
    FOREIGN KEY (CountryId) REFERENCES Country(CountryId),
    FOREIGN KEY (LocationTypeId) REFERENCES LocationType(LocationTypeId)
);

CREATE TABLE UserRole (
    UserRoleId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);

CREATE TABLE [User] (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    UserRoleId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE, -- Add unique constraint to Email
    CellPhoneNo NVARCHAR(30),
    AlternateCellPhoneNo NVARCHAR(30),
    PasswordHash NVARCHAR(256) NOT NULL,
    Salt NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    LastLogin DATETIME2 NULL,
    FOREIGN KEY (UserRoleId) REFERENCES UserRole(UserRoleId)
);

CREATE TABLE OperatorType (
    OperatorTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);

CREATE TABLE Operator (
    OperatorId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    OperatorTypeId INT NOT NULL,
    FOREIGN KEY (OperatorTypeId) REFERENCES OperatorType(OperatorTypeId)
);

CREATE TABLE Route (
    RouteId INT IDENTITY(1,1) PRIMARY KEY,
    OperatorId INT NOT NULL,
    FromId INT NOT NULL,
    ToId INT NOT NULL,
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedBy INT NULL,
    UpdatedDate DATETIME NULL,
    Active BIT NOT NULL DEFAULT 1,  
    Notes NVARCHAR(255) NULL,
    CONSTRAINT FK_Route_Operator FOREIGN KEY (OperatorId) REFERENCES Operator(OperatorId),
    CONSTRAINT FK_Route_Town_From FOREIGN KEY (FromId) REFERENCES Town(TownId),
    CONSTRAINT FK_Route_Town_To FOREIGN KEY (ToId) REFERENCES Town(TownId),
    CONSTRAINT FK_Route_User_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES [User](UserId),
    CONSTRAINT FK_Route_User_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES [User](UserId)
);

CREATE TABLE RouteTrip (
    TripId INT IDENTITY(1,1) PRIMARY KEY,
    RouteId INT NOT NULL,
    DepartureDateTime DATETIME NOT NULL,
    ArrivalDateTime DATETIME NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedBy INT NULL,
    UpdatedDate DATETIME NULL,
    Notes NVARCHAR(255) NULL,
    Active BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_RouteTrip_Route FOREIGN KEY (RouteId) REFERENCES Route(RouteId),
    CONSTRAINT FK_RouteTrip_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES [User](UserId),
    CONSTRAINT FK_RouteTrip_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES [User](UserId)
);


CREATE TABLE OperatorContact (
    OperatorContactId INT IDENTITY(1,1) PRIMARY KEY,
    OperatorId INT NOT NULL,
    Email NVARCHAR(100),
    CellPhoneNo NVARCHAR(30),
    Address NVARCHAR(200),
    OperatorContactPersonId INT NOT NULL,
    CONSTRAINT FK_OperatorContact_Operator FOREIGN KEY (OperatorId) REFERENCES Operator(OperatorId),
    CONSTRAINT FK_OperatorContact_Person FOREIGN KEY (OperatorContactPersonId) REFERENCES [User](UserId)
);

IF OBJECT_ID('Ticket', 'U') IS NULL
BEGIN
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
END
GO
