

SELECT * FROM RouteTrip 
where Active = 1


BEGIN TRAN
    UPDATE RouteTrip
    set DepartureDateTime = DATEADD(DAY,1,DepartureDateTime)
    WHERE RouteTrip.tripid IN
    (
    SELECT tripid FROM RouteTrip 
    where Active = 1
    )

SELECT * FROM RouteTrip 
where Active = 1



COMMIT
ROLLBACK