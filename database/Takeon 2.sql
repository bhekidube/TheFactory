INSERT INTO [dbo].[RouteTripTicketPrice] (
    OperatorId,
    RouteTripId,
    Price,
    Currency,
    EffectiveDate,
    Active
)
SELECT 
    O.OperatorId,
    RT.TripId,
    100.00 AS Price,  -- Set your default price here
    'ZAR' AS Currency, -- Use a single currency for all
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
);