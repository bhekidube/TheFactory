using System.Data;
using Microsoft.Data.SqlClient;
using TheFactory.Services;

namespace TheFactory.Services;

public sealed class TripHistoryArchiveService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TripHistoryArchiveService> _logger;
    private readonly TimeZoneInfo _timeZone;

    public TripHistoryArchiveService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<TripHistoryArchiveService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var timeZoneId = configuration["TripHistoryArchive:TimeZone"] ?? "Africa/Johannesburg";
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _timeZone);
            var nextRun = GetNextRunTime(localNow.DateTime);

            try
            {
                await Task.Delay(nextRun - localNow.DateTime, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ArchiveDueTripsAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(exception, "Scheduled trip history archive failed.");
            }
        }
    }

    public static DateTime GetNextRunTime(DateTime localNow)
    {
        var nextRun = localNow.Date.AddHours(17).AddMinutes(55);
        return localNow < nextRun ? nextRun : nextRun.AddDays(1);
    }

    private async Task ArchiveDueTripsAsync(CancellationToken cancellationToken)
    {
        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _timeZone);
        using var scope = _scopeFactory.CreateScope();
        var sqlConnectionService = scope.ServiceProvider.GetRequiredService<SqlConnectionService>();
        using var connection = await sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand("dbo.ArchiveDueTripHistory", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add("@ServiceDate", SqlDbType.Date).Value = localNow.Date;
        command.Parameters.Add("@LocalNow", SqlDbType.DateTime2).Value = localNow.DateTime;
        command.Parameters.Add("@UtcOffsetMinutes", SqlDbType.SmallInt).Value = (short)localNow.Offset.TotalMinutes;

        var archivedCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        _logger.LogInformation(
            "Scheduled trip history archive completed at {LocalTime}; archived {ArchivedCount} provisional trip occurrence(s).",
            localNow,
            archivedCount);
    }
}