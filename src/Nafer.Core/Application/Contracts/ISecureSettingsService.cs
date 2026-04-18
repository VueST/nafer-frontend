namespace Nafer.Core.Application.Contracts;

public interface ISecureSettingsService
{
    void Save<T>(string key, T value);
    T Get<T>(string key, T defaultValue = default!);
}
