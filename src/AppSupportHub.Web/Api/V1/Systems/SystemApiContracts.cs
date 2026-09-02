using AppSupportHub.Application.Systems.ReadModels;

namespace AppSupportHub.Web.Api.V1.Systems;

public sealed class SystemListRequest
{
    public string? Name { get; init; }

    public string? Type { get; init; }

    public string? Criticality { get; init; }

    public string? LifecycleStatus { get; init; }

    public int? Limit { get; init; }
}

public sealed record CreateSystemRequest(
    string? Name,
    string? Description,
    string? Type,
    string? Criticality,
    string? InitialLifecycleStatus,
    string? BusinessOwner,
    string? TechnicalOwner,
    string? SupportTeam,
    string? VendorName);

public sealed record UpdateSystemRequest(
    string? Name,
    string? Description,
    string? Type,
    string? Criticality,
    string? BusinessOwner,
    string? TechnicalOwner,
    string? SupportTeam,
    string? VendorName);

public sealed record ChangeSystemLifecycleRequest(
    string? TargetLifecycleStatus,
    string? RetirementReason);

public sealed record SystemSummaryResponse(
    Guid Id,
    string Name,
    string Type,
    string Criticality,
    string LifecycleStatus,
    string BusinessOwner,
    string TechnicalOwner,
    string SupportTeam,
    string? VendorName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static SystemSummaryResponse From(ApplicationSystemSummary system)
    {
        ArgumentNullException.ThrowIfNull(system);
        return new SystemSummaryResponse(
            system.Id,
            system.Name,
            system.TypeName,
            system.CriticalityName,
            system.LifecycleStatusName,
            system.BusinessOwner,
            system.TechnicalOwner,
            system.SupportTeam,
            system.VendorName,
            system.CreatedAtUtc,
            system.UpdatedAtUtc);
    }
}

public sealed record SystemDetailResponse(
    Guid Id,
    string Name,
    string Description,
    string Type,
    string Criticality,
    string LifecycleStatus,
    string BusinessOwner,
    string TechnicalOwner,
    string SupportTeam,
    string? VendorName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? RetiredAtUtc,
    string? RetirementReason)
{
    public static SystemDetailResponse From(ApplicationSystemDetail system)
    {
        ArgumentNullException.ThrowIfNull(system);
        return new SystemDetailResponse(
            system.Id,
            system.Name,
            system.Description,
            system.TypeName,
            system.CriticalityName,
            system.LifecycleStatusName,
            system.BusinessOwner,
            system.TechnicalOwner,
            system.SupportTeam,
            system.VendorName,
            system.CreatedAtUtc,
            system.UpdatedAtUtc,
            system.RetiredAtUtc,
            system.RetirementReason);
    }
}

public sealed record CreatedSystemResponse(Guid Id);

public sealed record MutationResponse(bool Changed);
