namespace Nafer.Core.Application.Contracts;

public interface ICredentialStorageService
{
    /// <summary>
    /// Securely saves a credential in the system's persistent store.
    /// </summary>
    void Save(string resource, string userName, string password);

    /// <summary>
    /// Retrieves a credential. Returns null if not found.
    /// </summary>
    string? Get(string resource, string userName);

    /// <summary>
    /// Removes a credential from the store.
    /// </summary>
    void Remove(string resource, string userName);

    /// <summary>
    /// Checks if a credential exists.
    /// </summary>
    bool Has(string resource, string userName);
}
