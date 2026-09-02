using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Domain.Systems;

namespace AppSupportHub.UnitTests.TestDoubles;

public sealed class InMemoryApplicationSystemRepository : IApplicationSystemRepository
{
    private readonly Dictionary<Guid, ApplicationSystem> _applicationSystems = [];

    public int GetByIdCallCount { get; private set; }

    public int NameExistsCallCount { get; private set; }

    public int AddCallCount { get; private set; }

    public CancellationToken GetByIdCancellationToken { get; private set; }

    public CancellationToken NameExistsCancellationToken { get; private set; }

    public CancellationToken AddCancellationToken { get; private set; }

    public IReadOnlyCollection<ApplicationSystem> Items => _applicationSystems.Values;

    public Task<ApplicationSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        GetByIdCallCount++;
        GetByIdCancellationToken = cancellationToken;
        _applicationSystems.TryGetValue(id, out ApplicationSystem? applicationSystem);
        return Task.FromResult(applicationSystem);
    }

    public Task<bool> NameExistsAsync(
        string normalizedName,
        CancellationToken cancellationToken)
    {
        NameExistsCallCount++;
        NameExistsCancellationToken = cancellationToken;
        bool exists = _applicationSystems.Values.Any(applicationSystem => string.Equals(
            applicationSystem.Name,
            normalizedName,
            StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task AddAsync(
        ApplicationSystem applicationSystem,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(applicationSystem);
        AddCallCount++;
        AddCancellationToken = cancellationToken;
        _applicationSystems.Add(applicationSystem.Id, applicationSystem);
        return Task.CompletedTask;
    }

    public void Seed(ApplicationSystem applicationSystem)
    {
        ArgumentNullException.ThrowIfNull(applicationSystem);
        _applicationSystems.Add(applicationSystem.Id, applicationSystem);
    }
}
