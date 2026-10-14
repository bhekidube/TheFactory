IF OBJECT_ID(N'institution.AttendanceSessions', N'U') IS NULL
BEGIN
    CREATE TABLE institution.AttendanceSessions
    (
        SessionID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AttendanceSessions PRIMARY KEY,
        ClassID int NOT NULL,
        SessionDate date NOT NULL,
        IsSubmitted bit NOT NULL CONSTRAINT DF_AttendanceSessions_IsSubmitted DEFAULT (0),
        CONSTRAINT FK_AttendanceSessions_Class
            FOREIGN KEY (ClassID) REFERENCES institution.[Class](Id),
        CONSTRAINT UQ_AttendanceSessions_ClassID_SessionDate UNIQUE (ClassID, SessionDate)
    );
END;
GO

IF OBJECT_ID(N'institution.AttendanceRecords', N'U') IS NULL
BEGIN
    CREATE TABLE institution.AttendanceRecords
    (
        RecordID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AttendanceRecords PRIMARY KEY,
        SessionID int NOT NULL,
        LearnerID int NOT NULL,
        Status nvarchar(10) NOT NULL CONSTRAINT DF_AttendanceRecords_Status DEFAULT (N'Present'),
        CONSTRAINT FK_AttendanceRecords_Session
            FOREIGN KEY (SessionID) REFERENCES institution.AttendanceSessions(SessionID) ON DELETE CASCADE,
        CONSTRAINT FK_AttendanceRecords_Learner
            FOREIGN KEY (LearnerID) REFERENCES institution.Learner(Id),
        CONSTRAINT UQ_AttendanceRecords_SessionID_LearnerID UNIQUE (SessionID, LearnerID),
        CONSTRAINT CK_AttendanceRecords_Status CHECK (Status IN (N'Present', N'Absent'))
    );
END;
GO
