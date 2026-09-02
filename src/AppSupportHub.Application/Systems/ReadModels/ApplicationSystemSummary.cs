using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.Systems.ReadModels;

public sealed record ApplicationSystemSummary(
    Guid Id,
    string Name,
    ApplicationSystemType Type,
    ApplicationCriticality Criticality,
    ApplicationLifecycleStatus LifecycleStatus,
    string BusinessOwner,
    string TechnicalOwner,
    string SupportTeam,
    string? VendorName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
