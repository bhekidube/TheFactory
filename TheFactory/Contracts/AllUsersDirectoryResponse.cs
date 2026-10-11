namespace TheFactory.Contracts;

/// <summary>Represents one user profile from the system-wide directory.</summary>
public sealed record AllUsersDirectoryResponse(
    int SourceId,
    string SourceType,
    string Name,
    string Email,
    string UserType,
    AddressDto Address);
