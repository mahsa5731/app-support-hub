using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.Queries;
using AppSupportHub.Application.Systems.ReadModels;

namespace AppSupportHub.Application.Systems.GetApplicationSystem;

public sealed class GetApplicationSystemHandler
{
    private readonly IApplicationSystemQueries _applicationSystemQueries;

    public GetApplicationSystemHandler(IApplicationSystemQueries applicationSystemQueries)
    {
        ArgumentNullException.ThrowIfNull(applicationSystemQueries);
        _applicationSystemQueries = applicationSystemQueries;
    }

    public async Task<ApplicationResult<ApplicationSystemDetail>> ExecuteAsync(
        GetApplicationSystemQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        ApplicationSystemDetail? applicationSystem = await _applicationSystemQueries.GetByIdAsync(
            query.ApplicationSystemId,
            cancellationToken);

        if (applicationSystem is null)
        {
            return ApplicationResultFactory.Failure<ApplicationSystemDetail>(new ApplicationError(
                "systems.not_found",
                "The application system was not found.",
                ApplicationErrorType.NotFound));
        }

        return ApplicationResultFactory.Success(applicationSystem);
    }
}
