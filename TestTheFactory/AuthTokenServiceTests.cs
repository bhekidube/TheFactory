namespace TestTheFactory;

using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TheFactory.Services;

public class AuthTokenServiceTests
{
    [Test]
    public void CreateToken_IncludesIdentityClaimsAndEightHourExpiry()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = new string('k', 32),
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-client"
            })
            .Build();
        var service = new AuthTokenService(configuration);
        var beforeIssue = DateTime.UtcNow;

        var token = new JwtSecurityTokenHandler().ReadJwtToken(
            service.CreateToken(27, "user@example.com", "Google User", "Customer", 9));

        Assert.That(token.Issuer, Is.EqualTo("test-issuer"));
        Assert.That(token.Audiences, Does.Contain("test-client"));
        Assert.That(token.Subject, Is.EqualTo("27"));
        Assert.That(token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Email).Value, Is.EqualTo("user@example.com"));
        Assert.That(token.Claims.Single(claim => claim.Type == ClaimTypes.Role).Value, Is.EqualTo("Customer"));
        Assert.That(token.Claims.Single(claim => claim.Type == "operatorId").Value, Is.EqualTo("9"));
        Assert.That(token.ValidTo, Is.GreaterThan(beforeIssue.AddHours(7)));
        Assert.That(token.ValidTo, Is.LessThanOrEqualTo(beforeIssue.AddHours(8).AddSeconds(2)));
    }

    [Test]
    public void CreateToken_RejectsMissingOrShortSigningKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "too-short"
            })
            .Build();
        var service = new AuthTokenService(configuration);

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateToken(27, "user@example.com", "Google User", "Customer", 9));
    }
}