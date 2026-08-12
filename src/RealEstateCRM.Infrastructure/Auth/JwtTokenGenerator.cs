using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Infrastructure.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(Guid userId, string fullName, Guid? companyId, IEnumerable<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // UI-display-only — never a security boundary, same as the role/company_id claims
            // already here. Without this, the client's only user identifier was the raw GUID
            // sub claim, which was rendering literally in the navbar (a real bug found while
            // visually verifying the running app — no endpoint existed for a user to look up
            // their own display name; GET /api/users is CompanyAdmin/SuperAdmin-only).
            new(JwtRegisteredClaimNames.Name, fullName),
        };

        if (companyId.HasValue)
        {
            claims.Add(new Claim("company_id", companyId.Value.ToString()));
        }

        // Deliberately the short "role" claim type, not ClaimTypes.Role (the long
        // http://schemas.microsoft.com/... URI) — matches docs/auth.md's documented JWT claim
        // shape ("role = Roles") exactly, and ASP.NET Core JWT Bearer's default inbound claim
        // mapping (JwtSecurityTokenHandler.DefaultInboundClaimTypeMap includes "role" ->
        // ClaimTypes.Role) means [Authorize(Roles=...)] server-side still works unchanged —
        // this was a real bug, not a stylistic choice: emitting the long URI meant the client's
        // JWT decode (which reads the token's raw claim keys directly, with no such mapping)
        // could never find a "role" property at all, so every browser session's user.roles was
        // silently an empty array — hiding every role-gated nav item/route (Billing, Users,
        // Company Settings, Commissions, WhatsApp Templates, Marketing Campaigns, API Keys,
        // Webhooks) from every user in the web app, for anyone, always. Found by actually
        // logging into the running app and navigating, not by any build/test/lint check.
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
