using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.Systems.CreateApplicationSystem;

public sealed record CreateApplicationSystemCommand(
    string Name,
    string Description,
    ApplicationSystemType Type,
    ApplicationCriticality Criticality,
    ApplicationLifecycleStatus InitialLifecycleStatus,
    string BusinessOwner,
    string TechnicalOwner,
    string SupportTeam,
    string? VendorName);
