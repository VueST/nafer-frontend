namespace Nafer.Core.Domain.Models;

/// <summary>
/// A UI-friendly, permission-aware view of the current user.
/// Derived from <see cref="AuthToken"/> — never hits the network.
/// Computed boolean flags allow clean XAML bindings without converter chains.
/// </summary>
public sealed record UserProfile(
    string Email,
    string DisplayName,
    string Initials,
    UserRole Role)
{
    /// <summary>
    /// True for Premium, Mod, and Admin tiers.
    /// Unlocks 4K quality, ad-free playback, and the Premium badge.
    /// </summary>
    public bool HasPremiumAccess =>
        Role is UserRole.Premium or UserRole.Mod or UserRole.Admin;

    /// <summary>
    /// True for Mod and Admin tiers.
    /// Unlocks comment deletion, temporary bans, and the Mod Dashboard.
    /// </summary>
    public bool CanModerate =>
        Role is UserRole.Mod or UserRole.Admin;

    /// <summary>
    /// True for Admin only.
    /// Unlocks content upload/removal, role changes, and the Admin Panel.
    /// </summary>
    public bool IsAdmin =>
        Role == UserRole.Admin;

    /// <summary>
    /// Display label for the role badge shown in the profile flyout.
    /// Returns null for the base User tier (no badge shown).
    /// </summary>
    public string? RoleBadgeLabel => Role switch
    {
        UserRole.Premium => "PREMIUM",
        UserRole.Mod     => "MOD",
        UserRole.Admin   => "ADMIN",
        _                => "USER"
    };

    /// <summary>
    /// Builds a <see cref="UserProfile"/> from an <see cref="AuthToken"/>.
    /// Single factory keeps derivation logic in one place.
    /// </summary>
    public static UserProfile FromToken(AuthToken token) => new(
        Email:       token.Email,
        DisplayName: BuildDisplayName(token.Email),
        Initials:    BuildInitials(token.Email),
        Role:        token.Role);

    private static string BuildDisplayName(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "Guest";
        var atIndex = email.IndexOf('@');
        var local = atIndex > 0 ? email[..atIndex] : email;
        return char.ToUpper(local[0]) + local[1..].ToLower();
    }

    private static string BuildInitials(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "G";
        return email[..1].ToUpperInvariant();
    }
}
