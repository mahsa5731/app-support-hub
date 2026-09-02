using AppSupportHub.Application.Abstractions.Persistence;

namespace AppSupportHub.UnitTests.TestDoubles;

public sealed class RecordingUnitOfWork : IUnitOfWork
{
    public int SaveCallCount { get; private set; }

    public int AffectedRecords { get; set; } = 1;

    public CancellationToken SaveCancellationToken { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCallCount++;
        SaveCancellationToken = cancellationToken;
        return Task.FromResult(AffectedRecords);
    }
}
