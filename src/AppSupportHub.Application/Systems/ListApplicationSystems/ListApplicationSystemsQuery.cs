using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.Systems.ListApplicationSystems;

public sealed record ListApplicationSystemsQuery(
    string? NameSearch = null,
    ApplicationSystemType? Type = null,
    ApplicationCriticality? Criticality = null,
    ApplicationLifecycleStatus? LifecycleStatus = null,
    int Limit = 50);
