using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;
using Nafer.Core.Application.Contracts;

namespace Nafer.Infrastructure.Services;

public class GitHubUpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private const string RepoUrl = "https://api.github.com/repos/VueST/Nafer/releases/latest";

    public event EventHandler<DownloadProgressArgs>? DownloadProgressChanged;

    public GitHubUpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Nafer-App");
    }

    public async Task<VersionInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(RepoUrl);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var node = JsonNode.Parse(json);
            if (node == null) return null;

            var latestVersion = new Version(node["tag_name"]?.ToString().TrimStart('v') ?? "0.0.0");
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

            if (latestVersion > currentVersion)
            {
                var asset = node["assets"]?.AsArray().FirstOrDefault();
                if (asset == null) return null;

                return new VersionInfo(
                    latestVersion, 
                    asset["browser_download_url"]?.ToString() ?? "",
                    node["body"]?.ToString() ?? ""
                );
            }
        }
        catch { /* Log error */ }
        return null;
    }

    public async Task DownloadUpdateAsync(VersionInfo version, string targetPath)
    {
        using var response = await _httpClient.GetAsync(version.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        var totalRead = 0L;
        int read;

        while ((read = await contentStream.ReadAsync(buffer)) != 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;

            if (totalBytes != -1)
            {
                var progress = (double)totalRead / totalBytes * 100;
                DownloadProgressChanged?.Invoke(this, new DownloadProgressArgs { ProgressPercentage = progress });
            }
        }
    }

    public void InstallAndRestart(string msiPath)
    {
        // Silent install per-user
        var psi = new ProcessStartInfo
        {
            FileName = "msiexec",
            Arguments = $"/i \"{msiPath}\" /qn /norestart",
            UseShellExecute = true
        };

        var process = Process.Start(psi);
        process?.WaitForExit();

        // Restart application using Environment.ProcessPath
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
        }
        
        Environment.Exit(0);
    }
}
