using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Nafer.Core.Application.Contracts;

namespace Nafer.Infrastructure.Persistence;

/// <summary>
/// Pro-grade settings persistence with DPAPI encryption and corruption resilience.
/// </summary>
[SupportedOSPlatform("windows")]
public class LocalSettingsService : ILocalSettingsService
{
    private readonly string _settingsPath;
    private Dictionary<string, object> _settings = new();
    private readonly HashSet<string> _sensitiveKeys = new() { "AuthToken", "RefreshToken" };
    private readonly ILogger<LocalSettingsService> _logger;

    public LocalSettingsService(ILogger<LocalSettingsService> logger)
    {
        _logger = logger;
        
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "Nafer");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");

        LoadSettings();
    }

    private void LoadSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            _settings = new();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            _settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Settings file corrupted. Resetting to defaults.");
            _settings = new();
            SaveSettings(); // Force-reset file
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading settings. Resetting.");
            _settings = new();
        }
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist settings.");
        }
    }

    public T Get<T>(string key, T defaultValue = default!)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            if (_sensitiveKeys.Contains(key) && value is string encryptedBase64)
            {
                try
                {
                    var decryptedString = DecryptString(encryptedBase64);
                    return JsonConvert.DeserializeObject<T>(decryptedString)!;
                }
                catch
                {
                    return defaultValue;
                }
            }

            if (value is T typedValue) return typedValue;
            
            try {
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value))!;
            } catch {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public void Save<T>(string key, T value)
    {
        if (_sensitiveKeys.Contains(key))
        {
            var json = JsonConvert.SerializeObject(value);
            _settings[key] = EncryptString(json);
        }
        else
        {
            _settings[key] = value!;
        }

        SaveSettings();
    }

    private static string EncryptString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    private static string DecryptString(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText)) return encryptedText;
        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
        byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
