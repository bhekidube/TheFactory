CREATE TABLE institution.Tenant (
 Id INT PRIMARY KEY,
 Name NVARCHAR(200),
 LogoUrl NVARCHAR(500)
)

CREATE TABLE institution.Learner (
 Id INT PRIMARY KEY,
 TenantId INT,
 FirstName NVARCHAR(100),
 Surname NVARCHAR(100),
 Grade NVARCHAR(50)
)

CREATE TABLE institution.Mark (
 Id INT PRIMARY KEY,
 TenantId INT,
 LearnerId INT,
 Subject NVARCHAR(100),
 Score INT
)