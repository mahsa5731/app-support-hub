using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.Systems.UpdateApplicationSystem;

public sealed record UpdateApplicationSystemCommand(
    Guid ApplicationSystemId,
    string Name,
    string Description,
    ApplicationSystemType Type,
    ApplicationCriticality Criticality,
    string BusinessOwner,
    string TechnicalOwner,
    string SupportTeam,
    string? VendorName);
