using System.Globalization;

namespace AppSupportHub.Web.Http;

public static class UtcInputParser
{
    private static readonly string[] _dateTimeLocalFormats =
        ["yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss"];

    public static bool TryParseDateTimeLocalUtc(
        string? value,
        out DateTimeOffset? parsedValue)
    {
        parsedValue = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!DateTime.TryParseExact(
            value.Trim(),
            _dateTimeLocalFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedDateTime))
        {
            return false;
        }

        parsedValue = new DateTimeOffset(DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Utc));
        return true;
    }

    public static bool TryParseIso8601WithOffset(
        string? value,
        out DateTimeOffset? parsedValue)
    {
        parsedValue = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        string normalizedValue = value.Trim();

        if (!HasExplicitOffset(normalizedValue)
            || !DateTimeOffset.TryParse(
                normalizedValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out DateTimeOffset parsedDateTime))
        {
            return false;
        }

        parsedValue = parsedDateTime;
        return true;
    }

    private static bool HasExplicitOffset(string value)
    {
        if (value.EndsWith('Z'))
        {
            return true;
        }

        int offsetStart = value.Length - 6;
        return offsetStart >= 0
            && value[offsetStart] is '+' or '-'
            && value[offsetStart + 3] == ':'
            && char.IsAsciiDigit(value[offsetStart + 1])
            && char.IsAsciiDigit(value[offsetStart + 2])
            && char.IsAsciiDigit(value[offsetStart + 4])
            && char.IsAsciiDigit(value[offsetStart + 5]);
    }
}
