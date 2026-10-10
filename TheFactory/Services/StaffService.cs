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
            @"SELECT Id, national_id AS NationalId, SchoolId, UserId, FirstName, Surname, Role, Email, Phone, Status, CreatedAt, UpdatedAt
              FROM institution.Staff
              WHERE SchoolId = @SchoolId
                AND (@Status = '' OR Status = @Status)
                AND (@Search = '' OR national_id LIKE @SearchPattern OR FirstName LIKE @SearchPattern OR Surname LIKE @SearchPattern OR Role LIKE @SearchPattern OR Email LIKE @SearchPattern OR Phone LIKE @SearchPattern)
              ORDER BY FirstName ASC, Surname ASC, national_id ASC;",
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

    public async Task<StaffDto?> GetStaffByNationalIdAsync(int schoolId, string nationalId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT Id, national_id AS NationalId, SchoolId, UserId, FirstName, Surname, Role, Email, Phone, Status, CreatedAt, UpdatedAt
              FROM institution.Staff
              WHERE national_id = @NationalId AND SchoolId = @SchoolId;",
            connection);
        command.Parameters.AddWithValue("@NationalId", nationalId);
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
            await EnsureEmailIsAvailableAsync(connection, transaction, schoolId, request.Email, null, null, cancellationToken);
            await EnsureNationalIdIsAvailableAsync(connection, transaction, request.NationalId, null, cancellationToken);
            var internalStaffId = await GetNextIdAsync(connection, transaction, cancellationToken);

            using var command = new SqlCommand(
                                @"INSERT INTO institution.Staff
                                        (Id, SchoolId, national_id, UserId, FirstName, Surname, Role, Email, Phone, Status, CreatedAt, UpdatedAt)
                  VALUES
                                        (@Id, @SchoolId, @NationalId, @UserId, @FirstName, @Surname, @Role, @Email, @Phone, @Status, SYSUTCDATETIME(), SYSUTCDATETIME());",
                connection,
                transaction);
            AddStaffParameters(command, internalStaffId, schoolId, request);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (await GetStaffByNationalIdAsync(schoolId, request.NationalId.Trim(), cancellationToken))!;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<StaffDto?> UpdateStaffAsync(int schoolId, string nationalId, StaffUpsertRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        await EnsureEmailIsAvailableAsync(connection, null, schoolId, request.Email, null, nationalId, cancellationToken);
        await EnsureNationalIdIsAvailableAsync(connection, null, request.NationalId, nationalId, cancellationToken);

        using var command = new SqlCommand(
            @"UPDATE institution.Staff
              SET national_id = @NationalId,
                  UserId = @UserId,
                  FirstName = @FirstName,
                  Surname = @Surname,
                  Role = @Role,
                  Email = @Email,
                  Phone = @Phone,
                  Status = @Status,
                  UpdatedAt = SYSUTCDATETIME()
              WHERE national_id = @ExistingNationalId AND SchoolId = @SchoolId;",
            connection);
        command.Parameters.AddWithValue("@ExistingNationalId", nationalId);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        AddStaffParameters(command, 0, schoolId, request, includeIdentity: false);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            return null;
        }

        return await GetStaffByNationalIdAsync(schoolId, request.NationalId.Trim(), cancellationToken);
    }

    public async Task<StaffDto?> UpdateLegacyStaffAsync(int schoolId, int legacyId, StaffUpsertRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        await EnsureEmailIsAvailableAsync(connection, null, schoolId, request.Email, legacyId, null, cancellationToken);
        await EnsureNationalIdIsAvailableAsync(connection, null, request.NationalId, null, cancellationToken);

        using var command = new SqlCommand(
            @"UPDATE institution.Staff
              SET national_id = @NationalId,
                  UserId = @UserId,
                  FirstName = @FirstName,
                  Surname = @Surname,
                  Role = @Role,
                  Email = @Email,
                  Phone = @Phone,
                  Status = @Status,
                  UpdatedAt = SYSUTCDATETIME()
              WHERE Id = @LegacyId AND SchoolId = @SchoolId AND national_id IS NULL;",
            connection);
        command.Parameters.AddWithValue("@LegacyId", legacyId);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        AddStaffParameters(command, 0, schoolId, request, includeIdentity: false);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            return null;
        }

        return await GetStaffByNationalIdAsync(schoolId, request.NationalId.Trim(), cancellationToken);
    }

    public async Task<bool> ArchiveStaffAsync(int schoolId, string nationalId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"UPDATE institution.Staff
              SET Status = 'Archived', UpdatedAt = SYSUTCDATETIME()
              WHERE national_id = @NationalId AND SchoolId = @SchoolId AND Status <> 'Archived';",
            connection);
        command.Parameters.AddWithValue("@NationalId", nationalId);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> ArchiveLegacyStaffAsync(int schoolId, int legacyId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"UPDATE institution.Staff
              SET Status = 'Archived', UpdatedAt = SYSUTCDATETIME()
              WHERE Id = @LegacyId AND SchoolId = @SchoolId AND national_id IS NULL AND Status <> 'Archived';",
            connection);
        command.Parameters.AddWithValue("@LegacyId", legacyId);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static StaffDto MapStaff(SqlDataReader reader)
    {
        return new StaffDto
        {
            LegacyId = reader.IsDBNull(1) ? reader.GetInt32(0) : null,
            NationalId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            SchoolId = reader.GetInt32(2),
            UserId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            FirstName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            Surname = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            Role = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            Email = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            Phone = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            Status = reader.IsDBNull(9) ? "Active" : reader.GetString(9),
            CreatedAt = reader.GetDateTime(10),
            UpdatedAt = reader.GetDateTime(11)
        };
    }

    private static void AddStaffParameters(SqlCommand command, int internalStaffId, int schoolId, StaffUpsertRequest request, bool includeIdentity = true)
    {
        if (includeIdentity)
        {
            command.Parameters.AddWithValue("@Id", internalStaffId);
            command.Parameters.AddWithValue("@SchoolId", schoolId);
        }

        command.Parameters.AddWithValue("@NationalId", request.NationalId.Trim());
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

    private static async Task EnsureEmailIsAvailableAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        int schoolId,
        string email,
        int? excludedStaffId,
        string? excludedNationalId,
        CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            @"SELECT COUNT(1) FROM institution.Staff
              WHERE SchoolId = @SchoolId AND LOWER(Email) = LOWER(@Email)
                AND (@ExcludedStaffId IS NULL OR Id <> @ExcludedStaffId)
                AND (@ExcludedNationalId IS NULL OR national_id <> @ExcludedNationalId);",
            connection,
            transaction);
        command.Parameters.AddWithValue("@SchoolId", schoolId);
        command.Parameters.AddWithValue("@Email", email.Trim());
        command.Parameters.AddWithValue("@ExcludedStaffId", (object?)excludedStaffId ?? DBNull.Value);
        command.Parameters.AddWithValue("@ExcludedNationalId", (object?)excludedNationalId ?? DBNull.Value);
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0)
        {
            throw new InvalidOperationException("A staff member with this email already exists in the school.");
        }
    }

    private static async Task EnsureNationalIdIsAvailableAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string nationalId,
        string? excludedNationalId,
        CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            @"SELECT COUNT(1) FROM institution.Staff
              WHERE national_id = @NationalId
                AND (@ExcludedNationalId IS NULL OR national_id <> @ExcludedNationalId);",
            connection,
            transaction);
        command.Parameters.AddWithValue("@NationalId", nationalId.Trim());
        command.Parameters.AddWithValue("@ExcludedNationalId", (object?)excludedNationalId ?? DBNull.Value);
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0)
        {
            throw new InvalidOperationException("A staff member with this national ID already exists.");
        }
    }
}