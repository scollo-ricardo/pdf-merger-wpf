using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace PDFMerger.Services;

public static class UpdateService
{
    private const string RepoOwner = "scollo-ricardo";
    private const string RepoName = "pdf-merger-wpf";

    private static readonly HttpClient _http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        // GitHub API requires a User-Agent.
        c.DefaultRequestHeaders.UserAgent.ParseAdd("PDFMerger-UpdateCheck/1.0");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return c;
    }

    public record UpdateInfo(Version LatestVersion, string ReleasePageUrl);

    /// <summary>
    /// Fetches the latest release from GitHub. Returns null if no update is
    /// available, the network call fails, or anything else goes wrong - this
    /// must never throw to the caller.
    /// </summary>
    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
            var json = await _http.GetStringAsync(url).ConfigureAwait(false);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagEl)) return null;
            if (!root.TryGetProperty("html_url", out var urlEl)) return null;

            var tag = tagEl.GetString();
            var htmlUrl = urlEl.GetString();
            if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(htmlUrl))
                return null;

            // Tags look like "v1.0.0"; strip the prefix before parsing.
            var stripped = tag.TrimStart('v', 'V');
            if (!Version.TryParse(stripped, out var latest)) return null;

            var current = Assembly.GetExecutingAssembly().GetName().Version
                          ?? new Version(0, 0, 0);

            // Normalize: compare only the parts we actually care about (Major.Minor.Build).
            var latestNorm = new Version(latest.Major, latest.Minor, Math.Max(0, latest.Build));
            var currentNorm = new Version(current.Major, current.Minor, Math.Max(0, current.Build));

            return latestNorm > currentNorm
                ? new UpdateInfo(latestNorm, htmlUrl)
                : null;
        }
        catch
        {
            return null;
        }
    }
}
