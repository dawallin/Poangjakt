using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Poangjakten.Web.Administration;

public sealed class AdminSessionService(IOptions<AdminOptions> options)
{
    public const string CookieName = "poangjakten.admin";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    private readonly AdminOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _sessions = new(StringComparer.Ordinal);

    public string DisplayName => string.IsNullOrWhiteSpace(_options.DisplayName) ? "Admin" : _options.DisplayName;

    public bool TrySignIn(string? suppliedSecret, out AdminSession? session)
    {
        session = null;
        if (string.IsNullOrWhiteSpace(_options.Secret) || string.IsNullOrEmpty(suppliedSecret) ||
            !SecretsMatch(_options.Secret, suppliedSecret))
        {
            return false;
        }

        RemoveExpiredSessions();
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTimeOffset.UtcNow.Add(SessionLifetime);
        _sessions[token] = expiresAt;
        session = new AdminSession(token, expiresAt, DisplayName);
        return true;
    }

    public bool IsAuthenticated(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var token) ||
            !_sessions.TryGetValue(token, out var expiresAt))
        {
            return false;
        }

        if (expiresAt > DateTimeOffset.UtcNow)
        {
            return true;
        }

        _sessions.TryRemove(token, out _);
        return false;
    }

    public void SignOut(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(CookieName, out var token))
        {
            _sessions.TryRemove(token, out _);
        }
    }

    private static bool SecretsMatch(string expected, string supplied)
    {
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        return CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash);
    }

    private void RemoveExpiredSessions()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var session in _sessions.Where(item => item.Value <= now))
        {
            _sessions.TryRemove(session.Key, out _);
        }
    }
}

public sealed record AdminSession(string Token, DateTimeOffset ExpiresAt, string DisplayName);
