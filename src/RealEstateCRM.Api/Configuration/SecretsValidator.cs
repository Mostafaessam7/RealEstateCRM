namespace RealEstateCRM.Api.Configuration;

/// <summary>
/// Refuses to start outside Development when required secrets are missing or still hold a
/// checked-in placeholder.
///
/// The trigger for this was <c>Jwt:Key</c> shipping as an empty string in <c>appsettings.json</c>.
/// That does not fail closed: the API starts normally in Production and only reaches the problem at
/// the first sign-in attempt. Whichever way that resolves — a signing exception on every login, or a
/// signing key that an attacker already knows is empty — neither is a state a deployment should be
/// able to reach silently. Multi-tenant SaaS makes the second case worse than usual: a forged token
/// carries a <c>CompanyId</c>, so it is not one account that leaks but any tenant the attacker names.
///
/// Matching is on placeholder <em>patterns</em> rather than a list of known values. A hardcoded list
/// only catches the placeholders someone remembered to enumerate and stops protecting the moment a
/// new one is written.
/// </summary>
public static class SecretsValidator
{
    private static readonly string[] PlaceholderMarkers =
    [
        "change_this", "change-this", "changethis",
        "change_me", "change-me", "changeme",
        "replace_me", "replace-me", "replaceme",
        "your-", "your_", "yourpassword",
        "placeholder", "example.com", "sample", "dummy", "todo",
        "development-key", "development_key", "dev-key", "dev_key",
        "test-key", "test_key", "secret123", "password123",
        "xxxx", "insert_", "insert-",
    ];

    /// <summary>HS256 signs with a 256-bit key; anything shorter weakens the signature.</summary>
    private const int MinimumSigningKeyLength = 32;

    /// <summary>Crude entropy proxy — a long key of one repeated character is still worthless.</summary>
    private const int MinimumDistinctCharacters = 12;

    public static void EnsureProductionSecretsAreConfigured(IConfiguration configuration, IHostEnvironment environment)
    {
        // Development keeps the checked-in defaults; that is what they are for. Every other
        // environment name — Staging, Production, or anything custom — takes the strict path.
        if (environment.IsDevelopment())
        {
            return;
        }

        var problems = new List<string>();

        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            problems.Add(
                "Jwt:Key is empty. The API would start and only fail at the first sign-in. "
                + "Supply it via the Jwt__Key environment variable or a secrets manager.");
        }
        else
        {
            if (jwtKey.Length < MinimumSigningKeyLength)
            {
                problems.Add($"Jwt:Key is shorter than {MinimumSigningKeyLength} characters, which weakens HS256 signing.");
            }

            if (jwtKey.Distinct().Count() < MinimumDistinctCharacters)
            {
                problems.Add("Jwt:Key has too little variation to be a real signing key.");
            }

            if (LooksLikePlaceholder(jwtKey))
            {
                problems.Add(
                    "Jwt:Key is still a checked-in placeholder, so it is published in the repository. "
                    + "In a multi-tenant deployment a forged token also carries a CompanyId, so this "
                    + "exposes every tenant, not one account.");
            }
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            problems.Add("ConnectionStrings:DefaultConnection is not set. Supply it via the ConnectionStrings__DefaultConnection environment variable.");
        }
        else if (LooksLikePlaceholder(connectionString))
        {
            problems.Add("ConnectionStrings:DefaultConnection still contains a placeholder value.");
        }

        // Optional integrations: validated only when configured, since each has a documented
        // no-op fallback (logging email sender, NoOp payment gateway) and running without them
        // is a supported deployment.
        RequireRealIfPresent(configuration, "Smtp:Password", problems);
        RequireRealIfPresent(configuration, "Stripe:SecretKey", problems);
        RequireRealIfPresent(configuration, "Stripe:WebhookSecret", problems);

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"Refusing to start in '{environment.EnvironmentName}' with unconfigured or placeholder secrets:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, problems.Select(p => "  - " + p))
                + Environment.NewLine
                + "See docs/deployment.md for how each value should be supplied.");
        }
    }

    private static void RequireRealIfPresent(IConfiguration configuration, string key, List<string> problems)
    {
        var value = configuration[key];
        if (!string.IsNullOrWhiteSpace(value) && LooksLikePlaceholder(value))
        {
            problems.Add($"{key} is still a checked-in placeholder. Supply a real value, or remove the section entirely to keep the integration disabled.");
        }
    }

    private static bool LooksLikePlaceholder(string value) =>
        PlaceholderMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
