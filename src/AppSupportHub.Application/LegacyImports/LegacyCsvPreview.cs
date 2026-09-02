using AppSupportHub.Application.Abstractions.Persistence;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.Systems.CreateApplicationSystem;
using AppSupportHub.Application.Systems.Inputs;
using AppSupportHub.Domain.Systems;

namespace AppSupportHub.Application.LegacyImports;

public sealed record LegacyCsvRawRow(
    int RowNumber,
    string LegacyId,
    string Name,
    string Description,
    string Type,
    string Criticality,
    string LifecycleStatus,
    string BusinessOwner,
    string TechnicalOwner,
    string SupportTeam,
    string? VendorName);

public sealed record LegacyCsvParseResult(
    IReadOnlyList<LegacyCsvRawRow> Rows,
    string? Error)
{
    public bool IsSuccess => Error is null;
}

public interface ILegacyCsvParser
{
    Task<LegacyCsvParseResult> ParseAsync(Stream content, CancellationToken cancellationToken);
}

public sealed record PreviewLegacyCsvCommand(Stream Content, long Length, string Extension);

public enum LegacyCsvDisposition
{
    Ready,
    ReviewDuplicate,
    Reject,
}

public sealed record LegacyCsvPreviewRow(
    int RowNumber,
    string LegacyId,
    string Name,
    bool IsValid,
    bool DuplicateLegacyIdInFile,
    bool DuplicateNameInFile,
    bool DuplicateNameInDatabase,
    IReadOnlyList<string> Errors,
    LegacyCsvDisposition Disposition);

public sealed record LegacyCsvPreview(IReadOnlyList<LegacyCsvPreviewRow> Rows)
{
    public int ReadyCount => Rows.Count(row => row.Disposition == LegacyCsvDisposition.Ready);

    public int ReviewDuplicateCount => Rows.Count(
        row => row.Disposition == LegacyCsvDisposition.ReviewDuplicate);

    public int RejectCount => Rows.Count(row => row.Disposition == LegacyCsvDisposition.Reject);
}

public sealed class PreviewLegacyCsvHandler(
    ILegacyCsvParser parser,
    ApplicationSystemInputFactory inputFactory,
    IApplicationSystemRepository systemRepository)
{
    public const long MaximumFileSize = 256 * 1024;

    public async Task<ApplicationResult<LegacyCsvPreview>> ExecuteAsync(
        PreviewLegacyCsvCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!command.Content.CanRead
            || command.Length is < 1 or > MaximumFileSize
            || !string.Equals(command.Extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return Invalid("Choose a non-empty .csv file no larger than 256 KiB.");
        }

        LegacyCsvParseResult parsed = await parser.ParseAsync(
            command.Content,
            cancellationToken);
        if (!parsed.IsSuccess)
        {
            return Invalid(parsed.Error!);
        }

        HashSet<string> duplicateLegacyIds = FindDuplicates(
            parsed.Rows.Select(row => row.LegacyId));
        HashSet<string> duplicateNames = FindDuplicates(parsed.Rows.Select(row => row.Name));
        var databaseNames = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var previewRows = new List<LegacyCsvPreviewRow>(parsed.Rows.Count);

        foreach (LegacyCsvRawRow row in parsed.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<string> errors = ValidateRow(row);
            string legacyId = row.LegacyId.Trim();
            string name = row.Name.Trim();
            bool duplicateLegacyId = duplicateLegacyIds.Contains(legacyId);
            bool duplicateName = duplicateNames.Contains(name);
            bool databaseDuplicate = false;

            if (errors.Count == 0 && !databaseNames.TryGetValue(name, out databaseDuplicate))
            {
                databaseDuplicate = await systemRepository.NameExistsAsync(
                    name,
                    null,
                    cancellationToken);
                databaseNames[name] = databaseDuplicate;
            }

            LegacyCsvDisposition disposition = errors.Count > 0
                ? LegacyCsvDisposition.Reject
                : duplicateLegacyId || duplicateName || databaseDuplicate
                    ? LegacyCsvDisposition.ReviewDuplicate
                    : LegacyCsvDisposition.Ready;
            previewRows.Add(new LegacyCsvPreviewRow(
                row.RowNumber,
                legacyId,
                name,
                errors.Count == 0,
                duplicateLegacyId,
                duplicateName,
                databaseDuplicate,
                errors,
                disposition));
        }

        return ApplicationResultFactory.Success(new LegacyCsvPreview(previewRows));
    }

    private List<string> ValidateRow(LegacyCsvRawRow row)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(row.LegacyId))
        {
            errors.Add("LegacyId is required.");
        }

        ApplicationResult<CreateApplicationSystemCommand> command =
            inputFactory.CreateCreateCommand(
                row.Name,
                row.Description,
                row.Type,
                row.Criticality,
                row.LifecycleStatus,
                row.BusinessOwner,
                row.TechnicalOwner,
                row.SupportTeam,
                row.VendorName);
        if (!command.IsSuccess)
        {
            errors.Add(command.Error!.Description);
            return errors;
        }

        try
        {
            CreateApplicationSystemCommand value = command.Value;
            _ = ApplicationSystem.Create(
                value.Name,
                value.Description,
                value.Type,
                value.Criticality,
                value.InitialLifecycleStatus,
                value.BusinessOwner,
                value.TechnicalOwner,
                value.SupportTeam,
                value.VendorName,
                DateTimeOffset.UnixEpoch);
        }
        catch (ArgumentException exception)
        {
            errors.Add(exception.Message);
        }

        return errors;
    }

    private static HashSet<string> FindDuplicates(IEnumerable<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static ApplicationResult<LegacyCsvPreview> Invalid(string description)
    {
        return ApplicationResultFactory.Failure<LegacyCsvPreview>(new ApplicationError(
            "validation.invalid_input",
            description,
            ApplicationErrorType.Validation));
    }
}
