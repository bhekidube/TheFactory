SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Create a stored procedure to create a user
ALTER PROCEDURE [dbo].[GetActiveTripsForDateFromLocationToDestination]
    @OperatorId NVARCHAR(50),
    @FromId NVARCHAR(50),
    @ToId NVARCHAR(50),
    @Date NVARCHAR(50)
AS
BEGIN
    SELECT O.Name, frm.Name,lTo.Name,[Date],DepartureTime FROM Route R 
        INNER JOIN Operator O ON R.OperatorId = O.OperatorId
        INNER JOIN [Location] frm ON frm.LocationId = R.FromId
        INNER JOIN [Location] lTo on lTo.LocationId = r.ToId
    WHERE O.OperatorId = @OperatorId AND FromId = @FromId and [Date] = @Date and ToId = @ToId and R.ACTIVE = 1
END;
GO
