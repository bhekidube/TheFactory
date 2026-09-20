using Microsoft.Data.SqlClient;
using System.Data;
using TheFactory.Contracts;

namespace TheFactory.Services;

public sealed class StaffService : IStaffService
{
    private readonly SqlConnectionService _sqlConnectionService;

    public StaffService(SqlConnectionService sqlConnectionService)
    {
        _sqlConnectionService = sqlConnectionService;
    }

    public async Task<IReadOnlyCollection<StaffDto>> GetStaffAsync(
        int schoolId,
        string? search,
        string? status,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT Id, SchoolId, UserId, FirstName, Surname, Role, Email, Phone, Status, CreatedAt, UpdatedAt
              FROM institution.Staff
              WHERE SchoolId = @SchoolId
                AND (@Status = '' OR Status = @Status)
                AND (@Search = '' OR FirstName LIKE @SearchPattern OR Surname LIKE @SearchPattern OR Role LIKE @SearchPattern OR Email LIKE @SearchPattern OR Phone LIKE @SearchPattern)
              ORDER BY FirstName ASC, Surname ASC, Id ASC;",
            connection);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        command.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(status) ? "Active" : status.Trim());
        var searchTerm = search?.Trim() ?? string.Empty;
        command.Parameters.AddWithValue("@Search", searchTerm);
        command.Parameters.AddWithValue("@SearchPattern", $"%{searchTerm}%");

        var staff = new List<StaffDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            staff.Add(MapStaff(reader));
        }

        return staff;
    }

    public async Task<StaffDto?> GetStaffByIdAsync(int schoolId, int staffId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT Id, SchoolId, UserId, FirstName, Surname, Role, Email, Phone, Status, CreatedAt, UpdatedAt
              FROM institution.Staff
              WHERE Id = @StaffId AND SchoolId = @SchoolId;",
            connection);
        command.Parameters.AddWithValue("@StaffId", staffId);
        command.Parameters.AddWithValue("@SchoolId", schoolId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapStaff(reader) : null;
    }

    public async Task<StaffDto> CreateStaffAsync(int schoolId, StaffUpsertRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            await EnsureSchoolExistsAsync(connection, transaction, schoolId, cancellationToken);
            await EnsureEmailIsAvailableAsync(connection, transaction, schoolId, request.Email, null, cancellationToken);
            var staffId = await GetNextIdAsync(connection, transaction, cancellationToken);

            using var command = new SqlCommand(
                                @"INSERT INTO institution.Staff
                                        (Id, SchoolId, UserId, FirstName, Surname, Role, Email, Phone, Status, CreatedAt, UpdatedAt)
                  VALUES
                                        (@Id, @SchoolId, @UserId, @FirstName, @Surname, @Role, @Email, @Phone, @Status, SYSUTCDATETIME(), SYSUTCDATETIME());",
                connection,
                transaction);
            AddStaffParameters(command, staffId, schoolId, request);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (await GetStaffByIdAsync(schoolId, staffId, cancellationToken))!;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<StaffDto?> UpdateStaffAsync(int schoolId, int staffId, StaffUpsertRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        await EnsureEmailIsAvailableAsync(connection, null, schoolId, request.Email, staffId, cancellationToken);

        using var command = new SqlCommand(
            @"UPDATE institution.Staff
              SET UserId = @UserId,
                  FirstName = @FirstName,
                  Surname = @Surname,
                  Role = @Role,
                  Email = @Email,
                  Phone = @Phone,
                  Status = @Status,
                  UpdatedAt = SYSUTCDATETIME()
              WHERE Id = @StaffId AND SchoolId = @SchoolId;",
            connection);
        command.Parameters.AddWithValue("@StaffId", staffId);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        AddStaffParameters(command, staffId, schoolId, request, includeIdentity: false);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            return null;
        }

        return await GetStaffByIdAsync(schoolId, staffId, cancellationToken);
    }

    public async Task<bool> ArchiveStaffAsync(int schoolId, int staffId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"UPDATE institution.Staff
              SET Status = 'Archived', UpdatedAt = SYSUTCDATETIME()
              WHERE Id = @StaffId AND SchoolId = @SchoolId AND Status <> 'Archived';",
            connection);
        command.Parameters.AddWithValue("@StaffId", staffId);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static StaffDto MapStaff(SqlDataReader reader)
    {
        return new StaffDto
        {
            Id = reader.GetInt32(0),
            SchoolId = reader.GetInt32(1),
            UserId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
            FirstName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            Surname = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            Role = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            Email = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            Phone = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            Status = reader.IsDBNull(8) ? "Active" : reader.GetString(8),
            CreatedAt = reader.GetDateTime(9),
            UpdatedAt = reader.GetDateTime(10)
        };
    }

    private static void AddStaffParameters(SqlCommand command, int staffId, int schoolId, StaffUpsertRequest request, bool includeIdentity = true)
    {
        if (includeIdentity)
        {
            command.Parameters.AddWithValue("@Id", staffId);
            command.Parameters.AddWithValue("@SchoolId", schoolId);
        }

        command.Parameters.AddWithValue("@UserId", (object?)request.UserId ?? DBNull.Value);
        command.Parameters.AddWithValue("@FirstName", request.FirstName.Trim());
        command.Parameters.AddWithValue("@Surname", request.Surname.Trim());
        command.Parameters.AddWithValue("@Role", request.Role.Trim());
        command.Parameters.AddWithValue("@Email", request.Email.Trim());
        command.Parameters.AddWithValue("@Phone", request.Phone.Trim());
        command.Parameters.AddWithValue("@Status", NormalizeStatus(request.Status));
    }

    private static string NormalizeStatus(string? status)
    {
        return string.Equals(status?.Trim(), "Archived", StringComparison.OrdinalIgnoreCase)
            ? "Archived"
            : "Active";
    }

    private static async Task<int> GetNextIdAsync(SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand("SELECT ISNULL(MAX(Id), 0) + 1 FROM institution.Staff;", connection, transaction);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task EnsureSchoolExistsAsync(SqlConnection connection, SqlTransaction transaction, int schoolId, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand("SELECT COUNT(1) FROM institution.Tenant WHERE Id = @SchoolId;", connection, transaction);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
        {
            throw new InvalidOperationException("School was not found.");
        }
    }

    private static async Task EnsureEmailIsAvailableAsync(SqlConnection connection, SqlTransaction? transaction, int schoolId, string email, int? excludedStaffId, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            @"SELECT COUNT(1) FROM institution.Staff
              WHERE SchoolId = @SchoolId AND LOWER(Email) = LOWER(@Email)
                AND (@ExcludedStaffId IS NULL OR Id <> @ExcludedStaffId);",
            connection,
            transaction);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        command.Parameters.AddWithValue("@Email", email.Trim());
        command.Parameters.AddWithValue("@ExcludedStaffId", (object?)excludedStaffId ?? DBNull.Value);
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0)
        {
            throw new InvalidOperationException("A staff member with this email already exists in the school.");
        }
    }
}