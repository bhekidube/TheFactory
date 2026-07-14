declare @DepartureDate DATETIME=GetDate(),
        @FromId INT=1, 
        @ToId INT =4              
                
                SELECT 
                    R.RouteId AS Route_RouteId,
                    O.OperatorId AS Operator_OperatorId,
                    O.Name AS OperatorName,
                    t.Name AS DestinationTown,
                    toL.Name AS BusStation,
                    FORMAT(GETDATE(), 'MMM d, yyyy') AS FormattedDate,
                    RT.TripId,
                    RT.RouteId,
                    CASE WHEN RT.Notes LIKE '%Daily%' THEN DATEADD(SECOND, DATEDIFF(SECOND, 0, CAST(RT.DepartureDateTime AS time)), CAST(@DepartureDate AS datetime)) ELSE RT.DepartureDateTime END AS DepartureDateTime,
                    RT.ArrivalDateTime,
                    RT.Price,
                    RT.CreatedBy,
                    RT.CreatedDate,
                    RT.UpdatedBy,
                    RT.UpdatedDate,
                    RT.Notes,
                    RT.Active

                FROM [dbo].[RouteTrip] RT
                INNER JOIN [ROUTE] R ON RT.RouteId = R.RouteId
                INNER JOIN Operator O ON O.OperatorId = R.OperatorId
                INNER JOIN [Location] toL on r.ToId = toL.LocationId
                INNER JOIN [Location] fromL on r.FromId = fromL.LocationId
                INNER JOIN [Town] T ON T.TownId = toL.TownId
                WHERE 
                (CAST(RT.DepartureDateTime AS DATE) = CAST(@DepartureDate AS DATE) OR RT.Notes LIKE '%Daily%')
                AND rt.Active = 1
                AND fromL.TownId IN (SELECT TownId FROM LOCATION WHERE LocationId = @FromId) 
                AND toL.TownId IN (SELECT TownId FROM LOCATION WHERE LocationId = @ToId)
                ORDER BY DATEADD(SECOND, DATEDIFF(SECOND, 0, CAST(RT.DepartureDateTime AS time)), CAST(@DepartureDate AS datetime)) ASC