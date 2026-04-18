using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Nafer.Core.Application.Contracts;

namespace Nafer.Infrastructure.Persistence;

/// <summary>
/// Hardened implementation of ISecureSettingsService using DPAPI for persistent state.
/// This ensures that settings are protected by the user's OS credentials.
/// </summary>
public sealed class SecureSettingsService : ISecureSettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nafer", "settings.json");

    private readonly Dictionary<string, string> _cache = new();

    public SecureSettingsService()
    {
        Load();
    }

    public void Save<T>(string key, T value)
    {
        try
        {
            var json = JsonConvert.SerializeObject(value);
            var unprotectedBytes = Encoding.UTF8.GetBytes(json);
            
            // Protect data using Windows DPAPI (Current User scope)
            var protectedBytes = ProtectedData.Protect(unprotectedBytes, null, DataProtectionScope.CurrentUser);
            
            _cache[key] = Convert.ToBase64String(protectedBytes);
            Persist();
        }
        catch (Exception ex)
        {
            // Fallback: log skip or handle
            System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to secure setting {key}: {ex.Message}");
        }
    }

    public T Get<T>(string key, T defaultValue = default!)
    {
        if (!_cache.TryGetValue(key, out var base64))
            return defaultValue;

        try
        {
            var bytesFromCache = Convert.FromBase64String(base64);
            
            var unprotectedBytes = ProtectedData.Unprotect(bytesFromCache, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(unprotectedBytes);
            
            return JsonConvert.DeserializeObject<T>(json) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    private void Load()
    {
        if (!File.Exists(SettingsPath)) return;

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (data != null)
            {
                foreach (var kvp in data) _cache[kvp.Key] = kvp.Value;
            }
        }
        catch
        {
            // File corrupted? Start fresh or keep empty cache.
        }
    }

    private void Persist()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
