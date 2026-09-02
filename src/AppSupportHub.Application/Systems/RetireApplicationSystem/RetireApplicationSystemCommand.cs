namespace AppSupportHub.Application.Systems.RetireApplicationSystem;

public sealed record RetireApplicationSystemCommand(
    Guid ApplicationSystemId,
    string RetirementReason);
