namespace Nafer.Core.Domain.Models;

/// <summary>
/// Represents the user's access tier.
/// Values MUST match the backend 'domain.UserRole' constants (case-insensitive parse).
/// </summary>
public enum UserRole
{
    User,
    Premium,
    Mod,
    Admin
}

/// <summary>
/// Immutable snapshot of a valid authentication session.
/// Contains both the short-lived access token and the long-lived refresh token.
/// The frontend uses ExpiresAt to pre-emptively refresh before expiry.
/// </summary>
public sealed record AuthToken(
    string Token,           // JWT access token (short-lived, e.g. 15m)
    string RefreshToken,    // Opaque UUID (long-lived, e.g. 7 days)
    string UserId,
    string Email,
    UserRole Role,
    DateTimeOffset ExpiresAt);  // Access token expiry — used for auto-refresh scheduling
