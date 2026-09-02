using System.Globalization;
using System.Text;
using AppSupportHub.Application.LegacyImports;
using CsvHelper;
using CsvHelper.Configuration;

namespace AppSupportHub.Infrastructure.LegacyImports;

public sealed class CsvHelperLegacyCsvParser : ILegacyCsvParser
{
    private static readonly string[] _requiredHeader =
    [
        "LegacyId", "Name", "Description", "Type", "Criticality", "LifecycleStatus",
        "BusinessOwner", "TechnicalOwner", "SupportTeam", "VendorName",
    ];

    public async Task<LegacyCsvParseResult> ParseAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        try
        {
            using var reader = new StreamReader(
                content,
                new UTF8Encoding(false, true),
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = _ => throw new FormatException(),
                HasHeaderRecord = true,
                IgnoreBlankLines = true,
            };
            using var csv = new CsvReader(reader, configuration);
            if (!await csv.ReadAsync())
            {
                return Failure("The CSV file is empty.");
            }

            csv.ReadHeader();
            if (csv.HeaderRecord is not { } header
                || !header.SequenceEqual(_requiredHeader, StringComparer.Ordinal))
            {
                return Failure("The CSV header does not match the required format.");
            }

            var rows = new List<LegacyCsvRawRow>();
            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (rows.Count == 100)
                {
                    return Failure("The CSV file cannot contain more than 100 data rows.");
                }

                rows.Add(new LegacyCsvRawRow(
                    csv.Parser.RawRow,
                    csv.GetField(0) ?? string.Empty,
                    csv.GetField(1) ?? string.Empty,
                    csv.GetField(2) ?? string.Empty,
                    csv.GetField(3) ?? string.Empty,
                    csv.GetField(4) ?? string.Empty,
                    csv.GetField(5) ?? string.Empty,
                    csv.GetField(6) ?? string.Empty,
                    csv.GetField(7) ?? string.Empty,
                    csv.GetField(8) ?? string.Empty,
                    csv.GetField(9)));
            }

            return rows.Count == 0
                ? Failure("The CSV file contains no data rows.")
                : new LegacyCsvParseResult(rows, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is CsvHelperException
            or DecoderFallbackException
            or FormatException)
        {
            return Failure(exception is DecoderFallbackException
                ? "The CSV file is not valid UTF-8."
                : "The CSV content is malformed.");
        }
    }

    private static LegacyCsvParseResult Failure(string error) => new([], error);
}
