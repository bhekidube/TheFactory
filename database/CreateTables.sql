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

CREATE TABLE UserType (
    UserTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);

CREATE TABLE [User] (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    UserTypeId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    CellPhoneNo NVARCHAR(30),
    AlternateCellPhoneNo NVARCHAR(30),
    FOREIGN KEY (UserTypeId) REFERENCES UserType(UserTypeId)
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
    Date DATE NOT NULL,
    DepartureTime NVARCHAR(10) NOT NULL,
    ArrivalTime NVARCHAR(10) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedBy INT NULL,
    UpdatedDate DATETIME NULL,
    FOREIGN KEY (OperatorId) REFERENCES Operator(OperatorId),
    FOREIGN KEY (FromId) REFERENCES Town(TownId),
    FOREIGN KEY (ToId) REFERENCES Town(TownId),
    FOREIGN KEY (CreatedBy) REFERENCES [User](UserId),
    FOREIGN KEY (UpdatedBy) REFERENCES [User](UserId)
);


CREATE TABLE OperatorContact (
    OperatorContactId INT IDENTITY(1,1) PRIMARY KEY,
    OperatorId INT NOT NULL,
    Email NVARCHAR(100),
    CellPhoneNo NVARCHAR(30),
    Address NVARCHAR(200),
    OperatorContactPersonId INT NOT NULL,
    FOREIGN KEY (OperatorId) REFERENCES Operator(OperatorId),
    FOREIGN KEY (OperatorContactPersonId) REFERENCES [User](UserId)
);