using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.Systems.Queries;

public sealed record ApplicationSystemQueryFilter(
    string? NameSearch,
    ApplicationSystemType? Type,
    ApplicationCriticality? Criticality,
    ApplicationLifecycleStatus? LifecycleStatus,
    int Limit);
