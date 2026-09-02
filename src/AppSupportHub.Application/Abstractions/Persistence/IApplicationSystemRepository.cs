using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.Abstractions.Persistence;

public interface IApplicationSystemRepository
{
    Task<ApplicationSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? excludedApplicationSystemId,
        CancellationToken cancellationToken);

    Task AddAsync(ApplicationSystem applicationSystem, CancellationToken cancellationToken);
}
