CREATE TABLE Country (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

CREATE TABLE Region (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    CountryId INT NOT NULL,
    FOREIGN KEY (CountryId) REFERENCES Country(Id)
);

CREATE TABLE Town (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    RegionId INT NOT NULL,
    FOREIGN KEY (RegionId) REFERENCES Region(Id)
);

CREATE TABLE LocationType (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);

CREATE TABLE Location (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    TownId INT NOT NULL,
    RegionId INT NOT NULL,
    CountryId INT NOT NULL,
    LocationTypeId INT NOT NULL,
    FOREIGN KEY (TownId) REFERENCES Town(Id),
    FOREIGN KEY (RegionId) REFERENCES Region(Id),
    FOREIGN KEY (CountryId) REFERENCES Country(Id),
    FOREIGN KEY (LocationTypeId) REFERENCES LocationType(Id)
);

CREATE TABLE UserType (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);

CREATE TABLE [User] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserTypeId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    CellPhoneNo NVARCHAR(30),
    AlternateCellPhoneNo NVARCHAR(30),
    FOREIGN KEY (UserTypeId) REFERENCES UserType(Id)
);

CREATE TABLE Route (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BusName NVARCHAR(100) NOT NULL,
    FromTownId INT NOT NULL,
    ToTownId INT NOT NULL,
    Date DATE NOT NULL,
    DepartureTime NVARCHAR(10) NOT NULL,
    ArrivalTime NVARCHAR(10) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    CreatedBy INT NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedBy INT NULL,
    UpdatedDate DATETIME NULL,
    FOREIGN KEY (FromTownId) REFERENCES Town(Id),
    FOREIGN KEY (ToTownId) REFERENCES Town(Id),
    FOREIGN KEY (CreatedBy) REFERENCES [User](Id),
    FOREIGN KEY (UpdatedBy) REFERENCES [User](Id)
);

CREATE TABLE OperatorType (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);

CREATE TABLE Operator (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    OperatorTypeId INT NOT NULL,
    FOREIGN KEY (OperatorTypeId) REFERENCES OperatorType(Id)
);



CREATE TABLE OperatorContact (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OperatorId INT NOT NULL,
    Email NVARCHAR(100),
    CellPhoneNo NVARCHAR(30),
    Address NVARCHAR(200),
    OperatorContactPersonId INT NOT NULL,
    FOREIGN KEY (OperatorId) REFERENCES Operator(Id),
    FOREIGN KEY (OperatorContactPersonId) REFERENCES [User](Id)
);