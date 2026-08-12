using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RealEstateCRM.Infrastructure.Auth;
using Xunit;

namespace RealEstateCRM.Tests.Auth;

public class JwtTokenGeneratorTests
{
    private static JwtTokenGenerator CreateGenerator(JwtOptions? options = null)
    {
        options ??= new JwtOptions
        {
            Key = "unit-test-signing-key-at-least-32-characters-long",
            Issuer = "RealEstateCRM.Tests",
            Audience = "RealEstateCRM.Tests",
            AccessTokenExpirationMinutes = 15
        };

        return new JwtTokenGenerator(Options.Create(options));
    }

    [Fact]
    public void GenerateAccessToken_IncludesSubjectCompanyAndRoleClaims()
    {
        var generator = CreateGenerator();
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var (token, expiresAt) = generator.GenerateAccessToken(userId, "Jane Doe", companyId, new[] { "CompanyAdmin" });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(userId.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(companyId.ToString(), jwt.Claims.Single(c => c.Type == "company_id").Value);
        // Must be the short "role" claim type — this is exactly what a client-side JWT decode
        // (raw payload, no .NET claim-type mapping applied) sees. A regression back to
        // ClaimTypes.Role here means the frontend can never find a "role" property on the
        // decoded token again, silently emptying every user's roles array client-side (a real
        // bug this test exists specifically to catch — see docs/auth.md#jwt-claims).
        Assert.Equal("CompanyAdmin", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal("Jane Doe", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.True(expiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateAccessToken_OmitsCompanyClaim_ForPlatformLevelUser()
    {
        var generator = CreateGenerator();

        var (token, _) = generator.GenerateAccessToken(Guid.NewGuid(), "Root Admin", companyId: null, new[] { "SuperAdmin" });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == "company_id");
    }

    [Fact]
    public void GenerateAccessToken_RoleClaim_StillResolvesToClaimTypesRole_AfterServerSideValidation()
    {
        // Proves the fix doesn't quietly break [Authorize(Roles = "...")] server-side: the short
        // "role" claim type must still round-trip to ClaimTypes.Role once the token goes through
        // the exact same validation pipeline Program.cs's JWT Bearer handler uses — relies on
        // JwtSecurityTokenHandler's default inbound claim-type mapping ("role" -> ClaimTypes.Role),
        // not just an assumption.
        var options = new JwtOptions
        {
            Key = "unit-test-signing-key-at-least-32-characters-long",
            Issuer = "RealEstateCRM.Tests",
            Audience = "RealEstateCRM.Tests",
            AccessTokenExpirationMinutes = 15,
        };
        var generator = new JwtTokenGenerator(Options.Create(options));

        var (token, _) = generator.GenerateAccessToken(Guid.NewGuid(), "Jane Doe", Guid.NewGuid(), new[] { "CompanyAdmin" });

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
        }, out _);

        Assert.True(principal.IsInRole("CompanyAdmin"));
        Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Role && c.Value == "CompanyAdmin");
    }
}
