using Newtonsoft.Json;
using Nafer.Core.Application.Contracts;

namespace Nafer.Infrastructure.Persistence;

public class LocalSettingsService : ILocalSettingsService
{
    private readonly string _settingsPath;
    private readonly Dictionary<string, object> _settings;

    public LocalSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "Nafer");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");

        if (File.Exists(_settingsPath))
        {
            var json = File.ReadAllText(_settingsPath);
            _settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new();
        }
        else
        {
            _settings = new();
        }
    }

    public T Get<T>(string key, T defaultValue = default!)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            if (value is T typedValue) return typedValue;
            
            // Handle JLong, JDouble etc if using Newtonsoft
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
        _settings[key] = value!;
        var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
        File.WriteAllText(_settingsPath, json);
    }
}
