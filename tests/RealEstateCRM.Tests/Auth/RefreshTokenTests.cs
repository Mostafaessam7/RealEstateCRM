using RealEstateCRM.Domain.Entities;
using Xunit;

namespace RealEstateCRM.Tests.Auth;

public class RefreshTokenTests
{
    [Fact]
    public void IsActive_False_WhenRevoked()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow
        };

        Assert.False(token.IsActive);
    }

    [Fact]
    public void IsActive_False_WhenExpired()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        Assert.False(token.IsActive);
    }

    [Fact]
    public void IsActive_True_WhenNotRevokedAndNotExpired()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        Assert.True(token.IsActive);
    }
}
