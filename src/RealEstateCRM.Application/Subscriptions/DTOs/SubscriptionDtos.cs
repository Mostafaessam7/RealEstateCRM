using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Application.Subscriptions.DTOs;

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxLeads { get; set; }
    public int MaxUnits { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSubscriptionPlanRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxLeads { get; set; }
    public int MaxUnits { get; set; }
}

public class UpdateSubscriptionPlanRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxLeads { get; set; }
    public int MaxUnits { get; set; }
    public bool IsActive { get; set; }
}

public class SubscriptionUsageDto
{
    public int UserCount { get; set; }
    public int LeadCount { get; set; }
    public int UnitCount { get; set; }
}

public class CompanySubscriptionDto
{
    public Guid Id { get; set; }
    public SubscriptionPlanDto Plan { get; set; } = null!;
    public SubscriptionStatus Status { get; set; }
    public DateTime TrialEndsAt { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? CancelledAt { get; set; }
    public SubscriptionUsageDto Usage { get; set; } = null!;
}

public class ChangePlanRequest
{
    public string PlanCode { get; set; } = string.Empty;
}
