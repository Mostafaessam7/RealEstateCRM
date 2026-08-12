using System.Security.Cryptography;
using System.Text;

namespace RealEstateCRM.Infrastructure.ApiKeys;

/// <summary>Generates and hashes API keys. Only the hash is ever persisted.</summary>
public static class ApiKeyHasher
{
    private const string KeyPrefixLabel = "rcrm_";

    public static string GenerateKey()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(randomBytes).Replace("+", "").Replace("/", "").Replace("=", "");
        return $"{KeyPrefixLabel}{token}";
    }

    public static string Hash(string plaintextKey)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintextKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>Short, non-secret identifier shown in the management UI (e.g. "rcrm_A1b2C3d4…").</summary>
    public static string DisplayPrefix(string plaintextKey) =>
        plaintextKey.Length <= 14 ? plaintextKey : plaintextKey[..14] + "…";
}
