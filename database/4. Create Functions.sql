--SELECT dbo.GetCurrentSASTTime() AS CurrentSASTTime;


CREATE FUNCTION dbo.GetCurrentSASTTime()
RETURNS DATETIME2
AS
BEGIN
    -- South Africa Standard Time is UTC+2, no daylight saving time
    RETURN DATEADD(HOUR, 2, SYSUTCDATETIME());
END;
GO
