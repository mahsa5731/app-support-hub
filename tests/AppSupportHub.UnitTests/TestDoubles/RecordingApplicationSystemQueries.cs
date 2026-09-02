using AppSupportHub.Application.Systems.Queries;
using AppSupportHub.Application.Systems.ReadModels;

namespace AppSupportHub.UnitTests.TestDoubles;

public sealed class RecordingApplicationSystemQueries : IApplicationSystemQueries
{
    public ApplicationSystemDetail? DetailResult { get; set; }

    public IReadOnlyList<ApplicationSystemSummary> ListResult { get; set; } = [];

    public Guid GetByIdId { get; private set; }

    public CancellationToken GetByIdCancellationToken { get; private set; }

    public ApplicationSystemQueryFilter? ListFilter { get; private set; }

    public CancellationToken ListCancellationToken { get; private set; }

    public Task<ApplicationSystemDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        GetByIdId = id;
        GetByIdCancellationToken = cancellationToken;
        return Task.FromResult(DetailResult);
    }

    public Task<IReadOnlyList<ApplicationSystemSummary>> ListAsync(
        ApplicationSystemQueryFilter filter,
        CancellationToken cancellationToken)
    {
        ListFilter = filter;
        ListCancellationToken = cancellationToken;
        return Task.FromResult(ListResult);
    }
}
