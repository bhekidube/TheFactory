SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Stored procedure to insert a new Route
CREATE PROCEDURE [dbo].[GetOperatorRoutes]
    @Active BIT,
    @OperatorId BIT
AS
BEGIN
        SELECT 
            O.Name Operator,  
            frm.Name [From],
            lTo.Name [To],
            CR.Name [CreatedBy],
            R.CreatedDate,
            UP.Name,
            R.UpdatedDate,
            R.Active,
            R.Notes
        FROM ROUTE R 
            INNER JOIN [Location] frm on R.FromId = frm.LocationId
            INNER JOIN [Location] lTo ON R.ToId = lTo.LocationId
            INNER JOIN [Operator] O ON O.OperatorId = R.OperatorId
            INNER JOIN [User] CR ON R.CreatedBy = CR.UserId
            LEFT JOIN [User] UP ON R.UpdatedBy = UP.UserId
        WHERE R.ACTIVE = @Active AND R.OperatorId = @OperatorId

        SELECT SCOPE_IDENTITY() AS NewRouteId;
END;
GO
