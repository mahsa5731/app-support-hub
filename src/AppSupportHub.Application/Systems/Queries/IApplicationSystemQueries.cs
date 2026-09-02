using AppSupportHub.Application.Systems.ReadModels;

namespace AppSupportHub.Application.Systems.Queries;

public interface IApplicationSystemQueries
{
    Task<ApplicationSystemDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ApplicationSystemSummary>> ListAsync(
        ApplicationSystemQueryFilter filter,
        CancellationToken cancellationToken);
}
