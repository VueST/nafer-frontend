using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Nafer.Core.Application.Contracts;
using Nafer.Core.Domain.Models;

namespace Nafer.Infrastructure.Services;

/// <summary>
/// HTTP implementation of IAuthService with race-condition protection and secure OAuth.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly ILogger<AuthService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthService(IHttpClientFactory httpClientFactory, ILogger<AuthService> logger)
    {
        _http = httpClientFactory.CreateClient("identity");
        _logger = logger;
    }

    // ── Public interface ──────────────────────────────────────────────────────

    public Task<AuthToken> LoginAsync(string email, string password) =>
        PostAuthAsync("api/v1/auth/login", new { email, password });

    public Task<AuthToken> RegisterAsync(string email, string password) =>
        PostAuthAsync("api/v1/auth/register", new { email, password });

    public async Task<AuthToken> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token cannot be empty.", nameof(refreshToken));

        await _refreshLock.WaitAsync();
        try
        {
            var response = await _http.PostAsJsonAsync(
                "api/v1/auth/refresh",
                new { refresh_token = refreshToken });

            await EnsureSuccessAsync(response, "Token refresh failed.");

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>()
                ?? throw new InvalidOperationException("Empty response from identity service.");

            return MapToToken(result);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// Implements Fix #6: Security-hardened browser-based login with state validation.
    /// </summary>
    public async Task<AuthToken> SecureLoginAsync(string oauthEndpoint, string clientId)
    {
        var state = Guid.NewGuid().ToString();
        var port = new Random().Next(5000, 60000);
        var redirectUri = $"http://127.0.0.1:{port}/callback";
        
        var authUrl = $"{oauthEndpoint}?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&state={state}";
        
        using var httpListener = new HttpListener();
        httpListener.Prefixes.Add($"{redirectUri}/");
        httpListener.Start();
        
        // Use default browser
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(authUrl) { UseShellExecute = true });
        
        var context = await httpListener.GetContextAsync();
        var request = context.Request;
        var response = context.Response;
        
        var receivedState = request.QueryString["state"];
        if (receivedState != state)
        {
            byte[] buffer = Encoding.UTF8.GetBytes("Invalid state parameter. Security validation failed.");
            response.StatusCode = 400;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            throw new InvalidOperationException("OAuth state mismatch. Potential CSRF attempt.");
        }
        
        var code = request.QueryString["code"];
        if (string.IsNullOrEmpty(code))
        {
            throw new InvalidOperationException("No authorization code received.");
        }
        
        byte[] successBuffer = Encoding.UTF8.GetBytes("<html><body><h1>Login successful</h1><p>You may close this window.</p></body></html>");
        await response.OutputStream.WriteAsync(successBuffer, 0, successBuffer.Length);
        response.Close();
        httpListener.Stop();
        
        return await ExchangeCodeForTokenAsync(code, redirectUri);
    }

    public async Task LogoutAsync(string accessToken, string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { refresh_token = refreshToken });

        try { await _http.SendAsync(request); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Remote logout failed. Local session will be cleared anyway.");
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<AuthToken> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        return await PostAuthAsync("api/v1/auth/token", new { code, redirect_uri = redirectUri, grant_type = "authorization_code" });
    }

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
