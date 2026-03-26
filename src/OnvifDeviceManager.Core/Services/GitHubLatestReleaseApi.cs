using System.Net.Http.Json;
using System.Text.Json;
using OnvifDeviceManager.Models;

namespace OnvifDeviceManager.Services;

/// <summary>Fetches the latest public GitHub Release and its downloadable assets.</summary>
public static class GitHubLatestReleaseApi
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/devildog5x5/ONVIF-ODM/releases/latest";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<(string? TagName, IReadOnlyList<ReleaseDownloadItem> Assets, string? Error)> TryGetLatestAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("ONVIF-ODM/1.0 (https://github.com/devildog5x5/ONVIF-ODM)");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var dto = await http.GetFromJsonAsync<GitHubReleaseDto>(LatestReleaseUrl, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (dto == null)
                return (null, Array.Empty<ReleaseDownloadItem>(), "Empty response from GitHub.");

            var list = new List<ReleaseDownloadItem>();
            if (dto.Assets != null)
            {
                foreach (var a in dto.Assets)
                {
                    if (string.IsNullOrWhiteSpace(a.Name) || string.IsNullOrWhiteSpace(a.BrowserDownloadUrl))
                        continue;
                    list.Add(new ReleaseDownloadItem { Name = a.Name, Url = a.BrowserDownloadUrl });
                }
            }

            return (dto.TagName, list, null);
        }
        catch (Exception ex)
        {
            return (null, Array.Empty<ReleaseDownloadItem>(), ex.Message);
        }
    }

    private sealed class GitHubReleaseDto
    {
        public string? TagName { get; set; }
        public List<GitHubAssetDto>? Assets { get; set; }
    }

    private sealed class GitHubAssetDto
    {
        public string? Name { get; set; }
        public string? BrowserDownloadUrl { get; set; }
    }
}
