using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.Systems.ChangeApplicationSystemLifecycle;

public sealed record ChangeApplicationSystemLifecycleCommand(
    Guid ApplicationSystemId,
    ApplicationLifecycleStatus TargetLifecycleStatus,
    string? RetirementReason = null);
