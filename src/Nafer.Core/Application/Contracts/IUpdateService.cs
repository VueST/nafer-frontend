namespace Nafer.Core.Application.Contracts;

public record VersionInfo(Version Version, string DownloadUrl, string ReleaseNotes);

public class DownloadProgressArgs : EventArgs
{
    public double ProgressPercentage { get; init; }
}

public interface IUpdateService
{
    event EventHandler<DownloadProgressArgs>? DownloadProgressChanged;
    
    Task<VersionInfo?> CheckForUpdatesAsync();
    Task DownloadUpdateAsync(VersionInfo version, string targetPath);
    void InstallAndRestart(string msiPath);
}
