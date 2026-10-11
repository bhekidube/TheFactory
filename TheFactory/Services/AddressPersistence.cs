using System.Data;
using Microsoft.Data.SqlClient;
using TheFactory.Contracts;

namespace TheFactory.Services;

internal static class AddressPersistence
{
    public static async Task<int?> GetOrCreateAddressIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        AddressDto? address,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(address);
        if (normalized is null)
        {
            return null;
        }

        using var findCommand = new SqlCommand(
            @"SELECT TOP (1) AddressId
              FROM institution.Addresses WITH (UPDLOCK, HOLDLOCK)
              WHERE UPPER(LTRIM(RTRIM(ISNULL(AddressLine1, N'')))) = UPPER(@AddressLine1)
                AND UPPER(LTRIM(RTRIM(ISNULL(AddressLine2, N'')))) = UPPER(@AddressLine2)
                AND UPPER(LTRIM(RTRIM(ISNULL(City, N'')))) = UPPER(@City)
                AND UPPER(LTRIM(RTRIM(ISNULL(ProvinceState, N'')))) = UPPER(@ProvinceState)
                AND UPPER(LTRIM(RTRIM(ISNULL(PostalCode, N'')))) = UPPER(@PostalCode)
                AND UPPER(LTRIM(RTRIM(ISNULL(Country, N'')))) = UPPER(@Country);",
            connection,
            transaction);
        AddAddressParameters(findCommand, normalized);

        var existingId = await findCommand.ExecuteScalarAsync(cancellationToken);
        if (existingId is not null && existingId != DBNull.Value)
        {
            return Convert.ToInt32(existingId);
        }

        using var insertCommand = new SqlCommand(
            @"INSERT INTO institution.Addresses
                (AddressLine1, AddressLine2, City, ProvinceState, PostalCode, Country)
              OUTPUT INSERTED.AddressId
              VALUES
                (@AddressLine1, @AddressLine2, @City, @ProvinceState, @PostalCode, @Country);",
            connection,
            transaction);
        AddAddressParameters(insertCommand, normalized);
        return Convert.ToInt32(await insertCommand.ExecuteScalarAsync(cancellationToken));
    }

    public static AddressDto ReadAddress(SqlDataReader reader, int startIndex)
    {
        return new AddressDto
        {
            AddressId = reader.IsDBNull(startIndex) ? null : reader.GetInt32(startIndex),
            AddressLine1 = reader.IsDBNull(startIndex + 1) ? string.Empty : reader.GetString(startIndex + 1),
            AddressLine2 = reader.IsDBNull(startIndex + 2) ? string.Empty : reader.GetString(startIndex + 2),
            City = reader.IsDBNull(startIndex + 3) ? string.Empty : reader.GetString(startIndex + 3),
            ProvinceState = reader.IsDBNull(startIndex + 4) ? string.Empty : reader.GetString(startIndex + 4),
            PostalCode = reader.IsDBNull(startIndex + 5) ? string.Empty : reader.GetString(startIndex + 5),
            Country = reader.IsDBNull(startIndex + 6) ? string.Empty : reader.GetString(startIndex + 6)
        };
    }

    private static AddressDto? Normalize(AddressDto? address)
    {
        if (address is null)
        {
            return null;
        }

        var normalized = new AddressDto
        {
            AddressLine1 = address.AddressLine1?.Trim() ?? string.Empty,
            AddressLine2 = address.AddressLine2?.Trim() ?? string.Empty,
            City = address.City?.Trim() ?? string.Empty,
            ProvinceState = address.ProvinceState?.Trim() ?? string.Empty,
            PostalCode = address.PostalCode?.Trim() ?? string.Empty,
            Country = address.Country?.Trim() ?? string.Empty
        };

        return string.IsNullOrWhiteSpace(normalized.AddressLine1)
            && string.IsNullOrWhiteSpace(normalized.AddressLine2)
            && string.IsNullOrWhiteSpace(normalized.City)
            && string.IsNullOrWhiteSpace(normalized.ProvinceState)
            && string.IsNullOrWhiteSpace(normalized.PostalCode)
            && string.IsNullOrWhiteSpace(normalized.Country)
                ? null
                : normalized;
    }

    private static void AddAddressParameters(SqlCommand command, AddressDto address)
    {
        command.Parameters.Add("@AddressLine1", SqlDbType.NVarChar, 200).Value = NullIfEmpty(address.AddressLine1);
        command.Parameters.Add("@AddressLine2", SqlDbType.NVarChar, 200).Value = NullIfEmpty(address.AddressLine2);
        command.Parameters.Add("@City", SqlDbType.NVarChar, 100).Value = NullIfEmpty(address.City);
        command.Parameters.Add("@ProvinceState", SqlDbType.NVarChar, 100).Value = NullIfEmpty(address.ProvinceState);
        command.Parameters.Add("@PostalCode", SqlDbType.NVarChar, 20).Value = NullIfEmpty(address.PostalCode);
        command.Parameters.Add("@Country", SqlDbType.NVarChar, 100).Value = NullIfEmpty(address.Country);
    }

    private static object NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    }
}
