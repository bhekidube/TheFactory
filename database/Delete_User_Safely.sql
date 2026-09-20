/*
    Delete_User_Safely.sql
    Purpose:
      - Safely delete a user from dbo.[User]
      - Handle dependent records in related tables
      - Preserve FK integrity by reassigning required references

    Usage:
      1) Set @UserIdToDelete and @ReassignUserId
      2) Run the preview section to inspect affected rows
      3) Run the delete transaction section
*/

SET NOCOUNT ON;

DECLARE @UserIdToDelete INT = 49;   -- TODO: replace with user to delete
DECLARE @ReassignUserId INT = 12;     -- TODO: replace with fallback user (must exist, cannot equal @UserIdToDelete)

/* ===============================
   Preview impacted rows
   =============================== */
SELECT 'User' AS RefTable, COUNT(*) AS Cnt
FROM dbo.[User] WHERE UserId = @UserIdToDelete
UNION ALL
SELECT 'Ticket.UserId', COUNT(*) FROM dbo.Ticket WHERE UserId = @UserIdToDelete
UNION ALL
SELECT 'SystemUserRole.UserId', COUNT(*) FROM dbo.SystemUserRole WHERE UserId = @UserIdToDelete
UNION ALL
SELECT 'OperatorUser.UserId', COUNT(*) FROM dbo.OperatorUser WHERE UserId = @UserIdToDelete
UNION ALL
SELECT 'OperatorUserRole via OperatorUser', COUNT(*)
FROM dbo.OperatorUserRole our
WHERE our.OperatorUserId IN (
    SELECT ou.OperatorUserId
    FROM dbo.OperatorUser ou
    WHERE ou.UserId = @UserIdToDelete
)
UNION ALL
SELECT 'OperatorContact.OperatorContactPersonId', COUNT(*) FROM dbo.OperatorContact WHERE OperatorContactPersonId = @UserIdToDelete
UNION ALL
SELECT 'Route.CreatedBy', COUNT(*) FROM dbo.Route WHERE CreatedBy = @UserIdToDelete
UNION ALL
SELECT 'Route.UpdatedBy', COUNT(*) FROM dbo.Route WHERE UpdatedBy = @UserIdToDelete
UNION ALL
SELECT 'RouteTrip.CreatedBy', COUNT(*) FROM dbo.RouteTrip WHERE CreatedBy = @UserIdToDelete
UNION ALL
SELECT 'RouteTrip.UpdatedBy', COUNT(*) FROM dbo.RouteTrip WHERE UpdatedBy = @UserIdToDelete;

/* ===============================
   Delete transaction
   =============================== */
BEGIN TRY
    BEGIN TRAN;

    IF NOT EXISTS (SELECT 1 FROM dbo.[User] WHERE UserId = @UserIdToDelete)
        THROW 50001, 'User to delete does not exist.', 1;

    IF @ReassignUserId IS NULL
        THROW 50002, 'Reassign user is required.', 1;

    IF @ReassignUserId = @UserIdToDelete
        THROW 50003, 'Reassign user cannot be the same as user being deleted.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.[User] WHERE UserId = @ReassignUserId)
        THROW 50004, 'Reassign user does not exist.', 1;

    -- Reassign required non-nullable references
    UPDATE dbo.Route
    SET CreatedBy = @ReassignUserId
    WHERE CreatedBy = @UserIdToDelete;

    UPDATE dbo.RouteTrip
    SET CreatedBy = @ReassignUserId
    WHERE CreatedBy = @UserIdToDelete;

    UPDATE dbo.OperatorContact
    SET OperatorContactPersonId = @ReassignUserId
    WHERE OperatorContactPersonId = @UserIdToDelete;

    -- Clear nullable references
    UPDATE dbo.Route
    SET UpdatedBy = NULL
    WHERE UpdatedBy = @UserIdToDelete;

    UPDATE dbo.RouteTrip
    SET UpdatedBy = NULL
    WHERE UpdatedBy = @UserIdToDelete;

    -- Remove child rows
    DELETE FROM dbo.Ticket
    WHERE UserId = @UserIdToDelete;

    DELETE our
    FROM dbo.OperatorUserRole our
    INNER JOIN dbo.OperatorUser ou ON ou.OperatorUserId = our.OperatorUserId
    WHERE ou.UserId = @UserIdToDelete;

    DELETE FROM dbo.OperatorUser
    WHERE UserId = @UserIdToDelete;

    DELETE FROM dbo.SystemUserRole
    WHERE UserId = @UserIdToDelete;

    -- Delete parent row
    DELETE FROM dbo.[User]
    WHERE UserId = @UserIdToDelete;

    COMMIT TRAN;

    SELECT
        'SUCCESS' AS [Status],
        @UserIdToDelete AS DeletedUserId,
        @ReassignUserId AS ReassignedToUserId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    SELECT
        'FAILED' AS [Status],
        ERROR_NUMBER() AS ErrorNumber,
        ERROR_MESSAGE() AS ErrorMessage,
        ERROR_LINE() AS ErrorLine;

    THROW;
END CATCH;
