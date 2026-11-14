using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TheFactory.Services;

public interface IAuthTokenService
{
    string CreateToken(int userId, string email, string userName, string userRole, int operatorId);
}

public sealed class AuthTokenService : IAuthTokenService
{
    private readonly IConfiguration _configuration;

    public AuthTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(int userId, string email, string userName, string userRole, int operatorId)
    {
        var signingKey = _configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 bytes.");
        }

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "hambaonline",
            audience: _configuration["Jwt:Audience"] ?? "hambaonline-client",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, userRole),
                new Claim("operatorId", operatorId.ToString())
            },
            notBefore: now,
            expires: now.AddHours(8),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}