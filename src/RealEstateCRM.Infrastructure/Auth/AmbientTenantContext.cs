namespace RealEstateCRM.Infrastructure.Auth;

/// <summary>
/// AsyncLocal-backed tenant id used only when there is no HttpContext (background jobs).
/// CurrentTenantService falls back to this after checking claims.
/// </summary>
internal static class AmbientTenantContext
{
    private static readonly AsyncLocal<Guid?> CurrentCompanyId = new();

    public static Guid? CompanyId => CurrentCompanyId.Value;

    public static IDisposable Begin(Guid companyId)
    {
        var previous = CurrentCompanyId.Value;
        CurrentCompanyId.Value = companyId;
        return new Restorer(previous);
    }

    private sealed class Restorer : IDisposable
    {
        private readonly Guid? _previous;
        public Restorer(Guid? previous) => _previous = previous;
        public void Dispose() => CurrentCompanyId.Value = _previous;
    }
}
