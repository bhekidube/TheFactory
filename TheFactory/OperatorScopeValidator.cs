using Microsoft.Data.SqlClient;

public static class OperatorScopeValidator
{
    public static bool ValidateOperatorMutationScope(int requestedOperatorId, int currentOperatorId)
    {
        return requestedOperatorId > 0 && currentOperatorId > 0 && requestedOperatorId == currentOperatorId;
    }

    public static async Task<int?> ResolveCurrentOperatorIdAsync(SqlConnection connection, int userId)
    {
        if (userId <= 0)
        {
            return null;
        }

        using var command = new SqlCommand(
            @"SELECT TOP 1 OU.OperatorId
              FROM [OperatorUser] OU
              WHERE OU.UserId = @UserId
              ORDER BY OU.OperatorId",
            connection);

        command.Parameters.AddWithValue("@UserId", userId);
        var value = await command.ExecuteScalarAsync();

        if (value is null || value is DBNull)
        {
            return null;
        }

        return Convert.ToInt32(value);
    }

    public static async Task<int?> ResolveRouteOperatorIdAsync(SqlConnection connection, int routeId)
    {
        if (routeId <= 0)
        {
            return null;
        }

        using var command = new SqlCommand(
            @"SELECT TOP 1 R.OperatorId
              FROM [Route] R
              WHERE R.RouteId = @RouteId",
            connection);

        command.Parameters.AddWithValue("@RouteId", routeId);
        var value = await command.ExecuteScalarAsync();

        if (value is null || value is DBNull)
        {
            return null;
        }

        return Convert.ToInt32(value);
    }

    public static async Task<int?> ResolveRouteTripOperatorIdAsync(SqlConnection connection, int routeTripId)
    {
        if (routeTripId <= 0)
        {
            return null;
        }

        using var command = new SqlCommand(
            @"SELECT TOP 1 R.OperatorId
              FROM [RouteTrip] RT
              INNER JOIN [Route] R ON R.RouteId = RT.RouteId
              WHERE RT.TripId = @TripId",
            connection);

        command.Parameters.AddWithValue("@TripId", routeTripId);
        var value = await command.ExecuteScalarAsync();

        if (value is null || value is DBNull)
        {
            return null;
        }

        return Convert.ToInt32(value);
    }

    public static string GetOperatorScopeErrorMessage(int requestedOperatorId, int currentOperatorId)
    {
        return $"The requested operator scope does not match the current session. Requested operator {requestedOperatorId}, current operator {currentOperatorId}.";
    }
}
