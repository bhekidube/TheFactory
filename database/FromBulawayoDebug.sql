




SELECT O.Name,rt.Price,fromLoc.Name FromStation ,fromTown.Name FromTown,toLoc.Name ToStation,toTown.Name ToTown,* FROM RouteTrip RT
INNER JOIN ROUTE R ON RT.RouteId = R.RouteId
INNER JOIN LOCATION fromLoc on fromLoc.LocationId = r.FromId
INNER JOIN LOCATION toLoc on toLoc.LocationId = r.toId
INNER JOIN OPERATOR O ON O.OperatorId = R.OperatorId
INNER JOIN TOWN fromTown ON fromTown.TOWNID = fromLoc.TownId
INNER JOIN TOWN toTown ON toTown.TOWNID = toLoc.TownId
WHERE 
RT.Active = 1
AND 
R.ACTIVE = 1
AND
fromTown.townId = 7 -- Bulawayo

/*begin TRAN
update route
set active = 1
where routeid = 18 

ROLLBACK

COMMIT*/

SELECT DISTINCT O.Name
FROM RouteTrip RT
INNER JOIN ROUTE R ON RT.RouteId = R.RouteId
INNER JOIN LOCATION fromLoc on fromLoc.LocationId = r.FromId
INNER JOIN LOCATION toLoc on toLoc.LocationId = r.toId
INNER JOIN OPERATOR O ON O.OperatorId = R.OperatorId
INNER JOIN TOWN fromTown ON fromTown.TOWNID = fromLoc.TownId
INNER JOIN TOWN toTown ON toTown.TOWNID = toLoc.TownId
WHERE 
RT.Active = 1
AND 
R.ACTIVE = 1
AND
fromTown.townId = 7 -- Bulawayo