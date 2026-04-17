namespace Nafer.Core.Application.Contracts;

public interface ILocalSettingsService
{
    T Get<T>(string key, T defaultValue = default!);
    void Save<T>(string key, T value);
}
