DECLARE @Today DATETIME= GETDATE();
DECLARE @FromTownId INT;
DECLARE @ToTownId INT;
DECLARE @trip TABLE ( TripId INT)

SELECT @FromTownId = TownId FROM Town WHERE Name = 'Johannesburg'
SELECT @ToTownId = TownId FROM Town WHERE Name = 'Bulawayo'

INSERT INTO @TRIP
SELECT TripID
FROM RouteTrip RT
INNER JOIN Route R ON RT.RouteId = R.RouteId
INNER JOIN Operator O ON O.OperatorId = R.OperatorId
INNER JOIN Location frm on frm.LocationId = R.fromId
INNER JOIN Location lTo on lTo.LocationId = R.ToId
where 
RT.Active = 1
AND frm.TownId = @FromTownId
AND lto.TownId = @ToTownId
AND DATEPART(day,DepartureDateTime)  = DATEPART(day,@Today)


    SELECT 
    O.Name
    ,frm.Name +' ('+ (SELECT TOP 1 Name FROM TOWN WHERE TownId = frm.TownId) + ')' [From],frm.TownId
    ,lTo.Name +' ('+ (SELECT TOP 1 Name FROM TOWN WHERE TownId = lTo.TownId) + ')' [To],lTo.TownId
    ,RT.* 
    ,lTo.Name 
    FROM RouteTrip RT
    INNER JOIN Route R ON RT.RouteId = R.RouteId
    INNER JOIN Operator O ON O.OperatorId = R.OperatorId
    INNER JOIN Location frm on frm.LocationId = R.fromId
    INNER JOIN Location lTo on lTo.LocationId = R.ToId
    INNER JOIN @trip  tr on tr.TripID = rt.TripID



BEGIN TRAN
    UPDATE RouteTrip
    set DepartureDateTime = DATEADD(DAY,1,@Today)
    WHERE 
    TripId IN (SELECT TripID FROM @TRIP)



    SELECT 
    O.Name
    ,frm.Name +' ('+ (SELECT TOP 1 Name FROM TOWN WHERE TownId = frm.TownId) + ')' [From],frm.TownId
    ,lTo.Name +' ('+ (SELECT TOP 1 Name FROM TOWN WHERE TownId = lTo.TownId) + ')' [To],lTo.TownId
    ,RT.* 
    ,lTo.Name 
    FROM RouteTrip RT
    INNER JOIN Route R ON RT.RouteId = R.RouteId
    INNER JOIN Operator O ON O.OperatorId = R.OperatorId
    INNER JOIN Location frm on frm.LocationId = R.fromId
    INNER JOIN Location lTo on lTo.LocationId = R.ToId
    INNER JOIN @trip  tr on tr.TripID = rt.TripID



COMMIT
ROLLBACK