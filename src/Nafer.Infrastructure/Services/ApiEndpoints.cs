namespace Nafer.Infrastructure.Services;

/// <summary>
/// All backend microservice base URLs — single source of truth.
/// Change here only when deploying to a different environment.
/// </summary>
public static class ApiEndpoints
{
    public const string Identity  = "https://nafer-backend.onrender.com/identity";
    public const string Media     = "https://nafer-backend.onrender.com/media";
    public const string Comment   = "https://nafer-backend.onrender.com/comment";
    public const string Notif     = "https://nafer-backend.onrender.com/notification";
    public const string Search    = "https://nafer-backend.onrender.com/search";
    public const string Streaming = "https://nafer-backend.onrender.com/stream";
}
