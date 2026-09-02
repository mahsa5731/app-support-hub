using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.Systems.ReadModels;

public sealed record ApplicationSystemDetail(
    Guid Id,
    string Name,
    string Description,
    ApplicationSystemType Type,
    ApplicationCriticality Criticality,
    ApplicationLifecycleStatus LifecycleStatus,
    string BusinessOwner,
    string TechnicalOwner,
    string SupportTeam,
    string? VendorName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? RetiredAtUtc,
    string? RetirementReason);
