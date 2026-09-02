using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Domain.Systems;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.Infrastructure.Persistence.Repositories;

public sealed class ApplicationSystemRepository : IApplicationSystemRepository
{
    private readonly AppSupportHubDbContext _dbContext;

    public ApplicationSystemRepository(AppSupportHubDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<ApplicationSystem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<ApplicationSystem>()
            .SingleOrDefaultAsync(applicationSystem => applicationSystem.Id == id, cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? excludedApplicationSystemId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(normalizedName);
        string trimmedName = normalizedName.Trim();

        IQueryable<ApplicationSystem> query = _dbContext.Set<ApplicationSystem>();

        if (excludedApplicationSystemId is { } excludedId)
        {
            query = query.Where(applicationSystem => applicationSystem.Id != excludedId);
        }

        return query.AnyAsync(
            applicationSystem => applicationSystem.Name == trimmedName,
            cancellationToken);
    }

    public async Task AddAsync(
        ApplicationSystem applicationSystem,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(applicationSystem);
        await _dbContext.Set<ApplicationSystem>().AddAsync(applicationSystem, cancellationToken);
    }
}
