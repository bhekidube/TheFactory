using Microsoft.Data.SqlClient;
using TheFactory.Contracts;

namespace TheFactory.Services;

public sealed class ReportService : IReportService
{
    private readonly ILearnerService _learnerService;
    private readonly SqlConnectionService _sqlConnectionService;

    public ReportService(ILearnerService learnerService, SqlConnectionService sqlConnectionService)
    {
        _learnerService = learnerService;
        _sqlConnectionService = sqlConnectionService;
    }

    public async Task<LearnerReportDto?> GetLearnerReportAsync(int learnerId, CancellationToken cancellationToken = default)
    {
        var learner = await _learnerService.GetLearnerByIdAsync(learnerId, cancellationToken);
        if (learner is null)
        {
            return null;
        }

        var subjects = await _learnerService.GetSubjectScoresAsync(learnerId, cancellationToken);
        var schoolName = await GetSchoolNameAsync(learnerId, cancellationToken);
        var average = subjects.Count == 0 ? 0 : subjects.Average(s => s.PupilMark);

        return new LearnerReportDto
        {
            SchoolName = schoolName,
            LearnerId = learner.Id,
            FirstName = learner.FirstName,
            Surname = learner.Surname,
            Grade = learner.Grade,
            Subjects = subjects,
            Average = Convert.ToDecimal(average)
        };
    }

    private async Task<string> GetSchoolNameAsync(int learnerId, CancellationToken cancellationToken)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT TOP (1) t.Name
              FROM institution.Learner l
              INNER JOIN institution.Tenant t ON t.Id = l.TenantId
              WHERE l.Id = @LearnerId;",
            connection);
        command.Parameters.AddWithValue("@LearnerId", learnerId);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is null || value == DBNull.Value)
        {
            return string.Empty;
        }

        return Convert.ToString(value) ?? string.Empty;
    }
}