namespace RealEstateCRM.Domain.Constants;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string CompanyAdmin = "CompanyAdmin";
    public const string SalesManager = "SalesManager";
    public const string SalesAgent = "SalesAgent";

    public static readonly string[] All =
    {
        SuperAdmin,
        CompanyAdmin,
        SalesManager,
        SalesAgent
    };
}
