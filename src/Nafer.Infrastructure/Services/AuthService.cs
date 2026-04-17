using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Nafer.Core.Application.Contracts;
using Nafer.Core.Domain.Models;

namespace Nafer.Infrastructure.Services;

/// <summary>
/// HTTP implementation of IAuthService.
/// Maps C# calls directly to identity-service REST endpoints.
///
/// API contract (identity-service):
///   POST /api/v1/auth/register  → { access_token, refresh_token, expires_at, user }
///   POST /api/v1/auth/login     → { access_token, refresh_token, expires_at, user }
///   POST /api/v1/auth/refresh   → { access_token, refresh_token, expires_at, user }
///   POST /api/v1/auth/logout    → 204 No Content  (requires Bearer token)
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly HttpClient _http;

    public AuthService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("identity");
    }

    // ── Public interface ──────────────────────────────────────────────────────

    public Task<AuthToken> LoginAsync(string email, string password) =>
        PostAuthAsync("/api/v1/auth/login", new { email, password });

    public Task<AuthToken> RegisterAsync(string email, string password) =>
        PostAuthAsync("/api/v1/auth/register", new { email, password });

    public async Task<AuthToken> RefreshAsync(string refreshToken)
    {
        // snake_case field name — matches backend JSON contract
        var response = await _http.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refresh_token = refreshToken });

        await EnsureSuccessAsync(response, "Token refresh failed.");

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Empty response from identity service.");

        return MapToToken(result);
    }

    public async Task LogoutAsync(string accessToken, string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { refresh_token = refreshToken });

        // Best-effort — swallow all errors. Local session clears regardless.
        try { await _http.SendAsync(request); }
        catch { /* network failures during logout are non-fatal */ }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<AuthToken> PostAuthAsync(string path, object body)
    {
        var response = await _http.PostAsJsonAsync(path, body);
        await EnsureSuccessAsync(response, "Authentication failed.");

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Empty response from identity service.");

        return MapToToken(result);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallback)
    {
        if (response.IsSuccessStatusCode) return;

        string? message = null;
        try
        {
            var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            message = err?.Error;
        }
        catch { /* ignore deserialization failures on error responses */ }

        throw new InvalidOperationException(message ?? fallback);
    }

    private static AuthToken MapToToken(AuthResponse r) => new(
        Token:        r.AccessToken,
        RefreshToken: r.RefreshToken,
        UserId:       r.User.Id,
        Email:        r.User.Email,
        Role:         ParseRole(r.User.Role),
        ExpiresAt:    DateTimeOffset.FromUnixTimeSeconds(r.ExpiresAt));

    private static UserRole ParseRole(string? roleStr) =>
        Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role)
            ? role
            : UserRole.User;

    // ── Private DTOs ─────────────────────────────────────────────────────────
    // [JsonPropertyName] is required because the backend uses snake_case
    // while System.Text.Json defaults to PascalCase matching.

    private sealed record AuthResponse(
        [property: JsonPropertyName("access_token")]  string  AccessToken,
        [property: JsonPropertyName("refresh_token")] string  RefreshToken,
        [property: JsonPropertyName("expires_at")]    long    ExpiresAt,
        [property: JsonPropertyName("user")]          UserDto User);

    private sealed record UserDto(
        [property: JsonPropertyName("id")]    string  Id,
        [property: JsonPropertyName("email")] string  Email,
        [property: JsonPropertyName("role")]  string? Role);

    private sealed record ErrorResponse(
        [property: JsonPropertyName("error")] string Error);
}
