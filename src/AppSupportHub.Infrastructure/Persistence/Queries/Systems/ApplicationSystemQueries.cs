using AppSupportHub.Application.Systems.Queries;
using AppSupportHub.Application.Systems.ReadModels;
using AppSupportHub.Domain.Systems;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.Infrastructure.Persistence.Queries.Systems;

public sealed class ApplicationSystemQueries : IApplicationSystemQueries
{
    private const string LikeEscapeCharacter = "\\";
    private readonly AppSupportHubDbContext _dbContext;

    public ApplicationSystemQueries(AppSupportHubDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<ApplicationSystemDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<ApplicationSystem>()
            .AsNoTracking()
            .Where(applicationSystem => applicationSystem.Id == id)
            .Select(applicationSystem => new ApplicationSystemDetail(
                applicationSystem.Id,
                applicationSystem.Name,
                applicationSystem.Description,
                applicationSystem.Type,
                applicationSystem.Criticality,
                applicationSystem.LifecycleStatus,
                applicationSystem.BusinessOwner,
                applicationSystem.TechnicalOwner,
                applicationSystem.SupportTeam,
                applicationSystem.VendorName,
                applicationSystem.CreatedAtUtc,
                applicationSystem.UpdatedAtUtc,
                applicationSystem.RetiredAtUtc,
                applicationSystem.RetirementReason))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationSystemSummary>> ListAsync(
        ApplicationSystemQueryFilter filter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        IQueryable<ApplicationSystem> query = _dbContext.Set<ApplicationSystem>().AsNoTracking();

        if (filter.NameSearch is not null)
        {
            string namePattern = $"%{EscapeLikePattern(filter.NameSearch)}%";
            query = query.Where(applicationSystem => EF.Functions.ILike(
                applicationSystem.Name,
                namePattern,
                LikeEscapeCharacter));
        }

        if (filter.Type is not null)
        {
            query = query.Where(applicationSystem => applicationSystem.Type == filter.Type);
        }

        if (filter.Criticality is not null)
        {
            query = query.Where(
                applicationSystem => applicationSystem.Criticality == filter.Criticality);
        }

        if (filter.LifecycleStatus is not null)
        {
            query = query.Where(
                applicationSystem => applicationSystem.LifecycleStatus == filter.LifecycleStatus);
        }

        return await query
            .OrderBy(applicationSystem => applicationSystem.Name)
            .ThenBy(applicationSystem => applicationSystem.Id)
            .Take(filter.Limit)
            .Select(applicationSystem => new ApplicationSystemSummary(
                applicationSystem.Id,
                applicationSystem.Name,
                applicationSystem.Type,
                applicationSystem.Criticality,
                applicationSystem.LifecycleStatus,
                applicationSystem.BusinessOwner,
                applicationSystem.TechnicalOwner,
                applicationSystem.SupportTeam,
                applicationSystem.VendorName,
                applicationSystem.CreatedAtUtc,
                applicationSystem.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(LikeEscapeCharacter, "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
