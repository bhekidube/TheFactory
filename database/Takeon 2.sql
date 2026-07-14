INSERT INTO [dbo].[RouteTripTicketPrice] (
    OperatorId,
    RouteTripId,
    Price,
    Currency,
    StartDate,
    EndDate,
    EffectiveDate,
    Active
)
SELECT 
    O.OperatorId,
    RT.TripId,
    100.00 AS Price,
    'ZAR' AS Currency,
    CAST('2024-01-01' AS DATETIME2) AS StartDate,   -- Set your desired start date
    CAST('2024-12-31' AS DATETIME2) AS EndDate,     -- Set your desired end date (or NULL for open-ended)
    SYSUTCDATETIME() AS EffectiveDate,
    1 AS Active
FROM [dbo].[Operator] O
INNER JOIN [dbo].[RouteTrip] RT ON RT.Active = 1
INNER JOIN [dbo].[Route] R ON RT.RouteId = R.RouteId AND R.Active = 1
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[RouteTripTicketPrice] RTP
    WHERE RTP.OperatorId = O.OperatorId 
      AND RTP.RouteTripId = RT.TripId
      AND RTP.Currency = 'ZAR'
      AND RTP.StartDate = CAST('2024-01-01' AS DATETIME2)
);