using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace TheFactory.Controllers;

[ApiController]
[Route("api/trip-analytics")]
public sealed class TripAnalyticsController : ControllerBase
{
    private readonly SqlConnectionService _sqlConnectionService;

    public TripAnalyticsController(SqlConnectionService sqlConnectionService)
    {
        _sqlConnectionService = sqlConnectionService;
    }

    [HttpPost("archive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ArchiveCompletedTrip(
        [FromBody] ArchiveCompletedTripRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TripId <= 0 || request.UserId <= 0 ||
            request.ServiceDate == default ||
            request.ActualDepartureDateTime == default ||
            request.ActualArrivalDateTime <= request.ActualDepartureDateTime ||
            request.ActualArrivalDateTime > DateTimeOffset.UtcNow)
        {
            return BadRequest(new { error = "A valid completed trip, service date, and actual departure and arrival times are required." });
        }

        var serviceDate = request.ServiceDate.Date;
        if (request.ActualDepartureDateTime.Date != serviceDate)
        {
            return BadRequest(new { error = "The service date must match the actual departure date." });
        }

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var tripCommand = new SqlCommand(
            @"SELECT RT.RouteId, R.OperatorId, RT.DepartureDateTime, RT.ArrivalDateTime, RT.Notes
              FROM dbo.RouteTrip RT
              INNER JOIN dbo.Route R ON R.RouteId = RT.RouteId
              WHERE RT.TripId = @TripId;",
            connection);
        tripCommand.Parameters.Add("@TripId", SqlDbType.Int).Value = request.TripId;

        int routeId;
        int operatorId;
        DateTime scheduledDeparture;
        DateTime scheduledArrival;
        string? notes;
        using (var reader = await tripCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                return NotFound(new { error = "The selected trip does not exist." });
            }

            routeId = reader.GetInt32(0);
            operatorId = reader.GetInt32(1);
            scheduledDeparture = reader.GetDateTime(2);
            scheduledArrival = reader.GetDateTime(3);
            notes = reader.IsDBNull(4) ? null : reader.GetString(4);
        }

        if (string.IsNullOrWhiteSpace(notes) || !notes.Contains("Daily", StringComparison.OrdinalIgnoreCase))
        {
            if (scheduledDeparture.Date != serviceDate)
            {
                return BadRequest(new { error = "The service date does not match the scheduled trip date." });
            }
        }
        else
        {
            var duration = scheduledArrival - scheduledDeparture;
            scheduledDeparture = serviceDate.Add(scheduledDeparture.TimeOfDay);
            scheduledArrival = scheduledDeparture.Add(duration);
        }

        if (!await IsOperatorUserAsync(connection, request.UserId, operatorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "The current user is not assigned to this operator." });
        }

        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            bool existingSalesComplete;
            using (var existingCommand = new SqlCommand(
                @"SELECT SalesDataComplete
                  FROM dbo.TripHistory WITH (UPDLOCK, HOLDLOCK)
                  WHERE TripId = @TripId AND ServiceDate = @ServiceDate;",
                connection,
                transaction))
            {
                existingCommand.Parameters.Add("@TripId", SqlDbType.Int).Value = request.TripId;
                existingCommand.Parameters.Add("@ServiceDate", SqlDbType.Date).Value = serviceDate;
                var existing = await existingCommand.ExecuteScalarAsync(cancellationToken);
                if (existing is not null)
                {
                    existingSalesComplete = Convert.ToBoolean(existing);
                    if (existingSalesComplete)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Conflict(new { error = "This trip occurrence has already been archived with complete sales data." });
                    }
                }
                else
                {
                    existingSalesComplete = false;
                }
            }

            int passengerVolume;
            decimal totalRevenue;
            using (var salesCommand = new SqlCommand(
                @"SELECT COUNT(1), COALESCE(SUM(Price), 0)
                  FROM dbo.Ticket
                  WHERE RouteTripId = @TripId AND TripServiceDate = @ServiceDate;",
                connection,
                transaction))
            {
                salesCommand.Parameters.Add("@TripId", SqlDbType.Int).Value = request.TripId;
                salesCommand.Parameters.Add("@ServiceDate", SqlDbType.Date).Value = serviceDate;
                using var reader = await salesCommand.ExecuteReaderAsync(cancellationToken);
                await reader.ReadAsync(cancellationToken);
                passengerVolume = reader.GetInt32(0);
                totalRevenue = reader.GetDecimal(1);
            }

            var saveSql = existingSalesComplete
                ? string.Empty
                : @"IF EXISTS (SELECT 1 FROM dbo.TripHistory WHERE TripId = @TripId AND ServiceDate = @ServiceDate)
                    BEGIN
                        UPDATE dbo.TripHistory
                        SET ScheduledDepartureDateTime = @ScheduledDepartureDateTime,
                            ScheduledArrivalDateTime = @ScheduledArrivalDateTime,
                            ActualDepartureDateTime = @ActualDepartureDateTime,
                            ActualArrivalDateTime = @ActualArrivalDateTime,
                            PassengerVolume = @PassengerVolume,
                            TotalRevenue = @TotalRevenue,
                            SalesDataComplete = @SalesDataComplete,
                            ArchivedBy = @ArchivedBy,
                            ArchivedAt = SYSUTCDATETIME()
                        WHERE TripId = @TripId AND ServiceDate = @ServiceDate AND SalesDataComplete = 0;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO dbo.TripHistory (
                            TripId, ServiceDate, OperatorId, RouteId,
                            ScheduledDepartureDateTime, ScheduledArrivalDateTime,
                            ActualDepartureDateTime, ActualArrivalDateTime,
                            PassengerVolume, TotalRevenue, SalesDataComplete, ArchivedBy)
                        VALUES (
                            @TripId, @ServiceDate, @OperatorId, @RouteId,
                            @ScheduledDepartureDateTime, @ScheduledArrivalDateTime,
                            @ActualDepartureDateTime, @ActualArrivalDateTime,
                            @PassengerVolume, @TotalRevenue, @SalesDataComplete, @ArchivedBy);
                    END;";

            using (var saveCommand = new SqlCommand(saveSql, connection, transaction))
            {
                saveCommand.Parameters.Add("@TripId", SqlDbType.Int).Value = request.TripId;
                saveCommand.Parameters.Add("@ServiceDate", SqlDbType.Date).Value = serviceDate;
                saveCommand.Parameters.Add("@OperatorId", SqlDbType.Int).Value = operatorId;
                saveCommand.Parameters.Add("@RouteId", SqlDbType.Int).Value = routeId;
                saveCommand.Parameters.Add("@ScheduledDepartureDateTime", SqlDbType.DateTime2).Value = scheduledDeparture;
                saveCommand.Parameters.Add("@ScheduledArrivalDateTime", SqlDbType.DateTime2).Value = scheduledArrival;
                saveCommand.Parameters.Add("@ActualDepartureDateTime", SqlDbType.DateTimeOffset).Value = request.ActualDepartureDateTime;
                saveCommand.Parameters.Add("@ActualArrivalDateTime", SqlDbType.DateTimeOffset).Value = request.ActualArrivalDateTime;
                saveCommand.Parameters.Add("@PassengerVolume", SqlDbType.Int).Value = passengerVolume;
                saveCommand.Parameters.Add("@TotalRevenue", SqlDbType.Decimal).Value = totalRevenue;
                saveCommand.Parameters["@TotalRevenue"].Precision = 18;
                saveCommand.Parameters["@TotalRevenue"].Scale = 2;
                saveCommand.Parameters.Add("@SalesDataComplete", SqlDbType.Bit).Value = request.SalesDataComplete;
                saveCommand.Parameters.Add("@ArchivedBy", SqlDbType.Int).Value = request.UserId;

                await saveCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return Ok(new
            {
                request.TripId,
                ServiceDate = serviceDate,
                OperatorId = operatorId,
                PassengerVolume = passengerVolume,
                TotalRevenue = totalRevenue,
                SalesDataComplete = request.SalesDataComplete
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    [HttpGet("operators/{operatorId:int}/daily")]
    public async Task<IActionResult> GetDailyHistory(
        int operatorId,
        [FromQuery] int userId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(operatorId, userId, fromDate, toDate);
        if (validation is not null)
        {
            return validation;
        }

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        if (!await IsOperatorUserAsync(connection, userId, operatorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        using var command = CreateRangeCommand(connection,
            @"SELECT H.ServiceDate, H.RouteId, COUNT(1) AS CompletedTrips,
                     SUM(H.PassengerVolume) AS PassengerVolume,
                     SUM(H.TotalRevenue) AS TotalRevenue
              FROM dbo.TripHistory H
              WHERE H.OperatorId = @OperatorId
                AND H.ServiceDate >= @FromDate AND H.ServiceDate <= @ToDate
                AND H.SalesDataComplete = 1
              GROUP BY H.ServiceDate, H.RouteId
              ORDER BY H.ServiceDate, H.RouteId;",
            operatorId, fromDate, toDate);

        var rows = new List<object>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new
            {
                ServiceDate = reader.GetDateTime(0),
                RouteId = reader.GetInt32(1),
                CompletedTrips = reader.GetInt32(2),
                PassengerVolume = reader.GetInt32(3),
                TotalRevenue = reader.GetDecimal(4)
            });
        }

        return Ok(rows);
    }

    [HttpGet("operators/{operatorId:int}/patterns")]
    public async Task<IActionResult> GetHistoricalPatterns(
        int operatorId,
        [FromQuery] int userId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(operatorId, userId, fromDate, toDate);
        if (validation is not null)
        {
            return validation;
        }

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        if (!await IsOperatorUserAsync(connection, userId, operatorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        using var command = CreateRangeCommand(connection,
            @"SELECT ((DATEDIFF(DAY, CONVERT(date, '19000101'), H.ServiceDate) % 7) + 1) AS DayOfWeek,
                     COUNT(1) AS CompletedTrips,
                     SUM(H.PassengerVolume) AS PassengerVolume,
                     SUM(H.TotalRevenue) AS TotalRevenue
              FROM dbo.TripHistory H
              WHERE H.OperatorId = @OperatorId
                AND H.ServiceDate >= @FromDate AND H.ServiceDate <= @ToDate
                AND H.SalesDataComplete = 1
              GROUP BY ((DATEDIFF(DAY, CONVERT(date, '19000101'), H.ServiceDate) % 7) + 1)
              ORDER BY DayOfWeek;

              SELECT MONTH(H.ServiceDate) AS MonthNumber,
                     COUNT(1) AS CompletedTrips,
                     SUM(H.PassengerVolume) AS PassengerVolume,
                     SUM(H.TotalRevenue) AS TotalRevenue
              FROM dbo.TripHistory H
              WHERE H.OperatorId = @OperatorId
                AND H.ServiceDate >= @FromDate AND H.ServiceDate <= @ToDate
                AND H.SalesDataComplete = 1
              GROUP BY MONTH(H.ServiceDate)
              ORDER BY MonthNumber;",
            operatorId, fromDate, toDate);

        var weekdays = new List<object>();
        var months = new List<object>();
        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                weekdays.Add(new
                {
                    DayOfWeek = reader.GetInt32(0),
                    CompletedTrips = reader.GetInt32(1),
                    PassengerVolume = reader.GetInt32(2),
                    TotalRevenue = reader.GetDecimal(3)
                });
            }

            await reader.NextResultAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                months.Add(new
                {
                    MonthNumber = reader.GetInt32(0),
                    CompletedTrips = reader.GetInt32(1),
                    PassengerVolume = reader.GetInt32(2),
                    TotalRevenue = reader.GetDecimal(3)
                });
            }
        }

        return Ok(new { WeekdayPatterns = weekdays, MonthlySeasonality = months });
    }

    [HttpGet("operators/{operatorId:int}/forecasting-data")]
    public async Task<IActionResult> GetForecastingData(
        int operatorId,
        [FromQuery] int userId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? routeId,
        CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(operatorId, userId, fromDate, toDate);
        if (validation is not null)
        {
            return validation;
        }

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        if (!await IsOperatorUserAsync(connection, userId, operatorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        using var command = CreateRangeCommand(connection,
            @"SELECT H.ServiceDate, H.RouteId, COUNT(1) AS CompletedTrips,
                     SUM(H.PassengerVolume) AS PassengerVolume,
                     SUM(H.TotalRevenue) AS TotalRevenue,
                     AVG(CAST(H.PassengerVolume AS DECIMAL(18,2))) AS AveragePassengersPerTrip,
                     AVG(H.TotalRevenue) AS AverageRevenuePerTrip,
                     CASE WHEN SUM(H.PassengerVolume) = 0 THEN CONVERT(DECIMAL(18,2), 0)
                         ELSE SUM(H.TotalRevenue) / SUM(H.PassengerVolume)
                     END AS AverageFarePerPassenger
              FROM dbo.TripHistory H
              WHERE H.OperatorId = @OperatorId
                AND H.ServiceDate >= @FromDate AND H.ServiceDate <= @ToDate
                AND H.SalesDataComplete = 1
                AND (@RouteId IS NULL OR H.RouteId = @RouteId)
              GROUP BY H.ServiceDate, H.RouteId
              ORDER BY H.ServiceDate, H.RouteId;",
            operatorId, fromDate, toDate);
        command.Parameters.Add("@RouteId", SqlDbType.Int).Value = routeId.HasValue ? routeId.Value : DBNull.Value;

        var rows = new List<object>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new
            {
                ServiceDate = reader.GetDateTime(0),
                RouteId = reader.GetInt32(1),
                CompletedTrips = reader.GetInt32(2),
                PassengerVolume = reader.GetInt32(3),
                TotalRevenue = reader.GetDecimal(4),
                AveragePassengersPerTrip = reader.GetDecimal(5),
                AverageRevenuePerTrip = reader.GetDecimal(6),
                AverageFarePerPassenger = reader.GetDecimal(7)
            });
        }

        return Ok(new { OperatorId = operatorId, FromDate = fromDate.Date, ToDate = toDate.Date, Series = rows });
    }

    private static IActionResult? ValidateRequest(int operatorId, int userId, DateTime fromDate, DateTime toDate)
    {
        if (!TripAnalyticsRequestValidation.IsValidRange(operatorId, userId, fromDate, toDate))
        {
            return new BadRequestObjectResult(new { error = "Valid operator, user, and date range values are required; the range may not exceed ten years." });
        }

        return null;
    }

    private static SqlCommand CreateRangeCommand(
        SqlConnection connection,
        string commandText,
        int operatorId,
        DateTime fromDate,
        DateTime toDate)
    {
        var command = new SqlCommand(commandText, connection);
        command.Parameters.Add("@OperatorId", SqlDbType.Int).Value = operatorId;
        command.Parameters.Add("@FromDate", SqlDbType.Date).Value = fromDate.Date;
        command.Parameters.Add("@ToDate", SqlDbType.Date).Value = toDate.Date;
        return command;
    }

    private static async Task<bool> IsOperatorUserAsync(
        SqlConnection connection,
        int userId,
        int operatorId,
        CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            @"SELECT CASE WHEN EXISTS (
                  SELECT 1 FROM dbo.OperatorUser
                  WHERE UserId = @UserId AND OperatorId = @OperatorId
              ) THEN 1 ELSE 0 END;",
            connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@OperatorId", SqlDbType.Int).Value = operatorId;
        return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
    }
}

public sealed class ArchiveCompletedTripRequest
{
    public int TripId { get; set; }
    public DateTime ServiceDate { get; set; }
    public DateTimeOffset ActualDepartureDateTime { get; set; }
    public DateTimeOffset ActualArrivalDateTime { get; set; }
    public int UserId { get; set; }
    public bool SalesDataComplete { get; set; }
}

public static class TripAnalyticsRequestValidation
{
    public static bool IsValidRange(int operatorId, int userId, DateTime fromDate, DateTime toDate)
    {
        return operatorId > 0 && userId > 0 && fromDate != default && toDate != default &&
            fromDate.Date <= toDate.Date && toDate.Date <= DateTime.UtcNow.Date &&
            (toDate.Date - fromDate.Date).TotalDays <= 3660;
    }
}