
SELECT O.Name,RT.* FROM RouteTrip RT
INNER JOIN Route R ON RT.RouteId = R.RouteId
INNER JOIN Operator O ON O.OperatorId = R.OperatorId
where RT.Active = 1


BEGIN TRAN
    UPDATE RouteTrip
    set DepartureDateTime = DATEADD(DAY,1,DepartureDateTime)
    WHERE RouteTrip.tripid IN
    (
    SELECT tripid FROM RouteTrip 
    where Active = 1
    )

SELECT O.Name,RT.* FROM RouteTrip RT
INNER JOIN Route R ON RT.RouteId = R.RouteId
INNER JOIN Operator O ON O.OperatorId = R.OperatorId
where RT.Active = 1



COMMIT
ROLLBACK