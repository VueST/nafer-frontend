using Windows.Security.Credentials;
using Nafer.Core.Application.Contracts;

namespace Nafer.Infrastructure.Services;

/// <summary>
/// Professional implementation of ICredentialStorageService using Windows PasswordVault (Credential Locker).
/// This provides OS-level encryption and links credentials to the Windows user profile.
/// </summary>
public sealed class WinCredentialStorageService : ICredentialStorageService
{
    private readonly PasswordVault _vault = new();

    public void Save(string resource, string userName, string password)
    {
        if (string.IsNullOrEmpty(resource) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            return;

        // Ensure we remove any existing credential for this user/resource first to avoid duplicates
        Remove(resource, userName);

        var credential = new PasswordCredential(resource, userName, password);
        _vault.Add(credential);
    }

    public string? Get(string resource, string userName)
    {
        try
        {
            var credential = _vault.Retrieve(resource, userName);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch
        {
            // Windows throws an exception if the credential isn't found.
            return null;
        }
    }

    public void Remove(string resource, string userName)
    {
        try
        {
            var credential = _vault.Retrieve(resource, userName);
            _vault.Remove(credential);
        }
        catch
        {
            // Credential not found, nothing to do.
        }
    }

    public bool Has(string resource, string userName)
    {
        try
        {
            _vault.Retrieve(resource, userName);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
