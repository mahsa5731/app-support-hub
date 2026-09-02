using System.Net;
using System.Text.RegularExpressions;

namespace AppSupportHub.IntegrationTests.Web;

internal static partial class AntiforgeryTokenExtractor
{
    internal static string Extract(string html)
    {
        Match match = AntiforgeryInputRegex().Match(html);
        Assert.True(match.Success, "The rendered form did not contain an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    [GeneratedRegex(
        "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
        RegexOptions.CultureInvariant)]
    private static partial Regex AntiforgeryInputRegex();
}
