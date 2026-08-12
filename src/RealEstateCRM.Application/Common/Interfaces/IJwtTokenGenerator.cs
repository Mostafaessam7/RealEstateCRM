namespace RealEstateCRM.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(Guid userId, string fullName, Guid? companyId, IEnumerable<string> roles);
}
