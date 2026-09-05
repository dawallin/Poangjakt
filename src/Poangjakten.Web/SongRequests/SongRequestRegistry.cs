using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Poangjakten.Web.SongRequests;

public sealed partial class SongRequestRegistry(
    ISongRequestRepository repository,
    ILogger<SongRequestRegistry> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, SongRequest> _requests = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var requests = await repository.LoadAllAsync(cancellationToken);
        foreach (var songRequest in requests)
        {
            _requests[songRequest.Id] = songRequest;
        }

        logger.LogInformation("Loaded {SongRequestCount} song requests into memory", requests.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public SongRequest? Find(string id) => _requests.GetValueOrDefault(id);

    public IReadOnlyList<SongRequest> List() => _requests.Values
        .OrderByDescending(songRequest => songRequest.RequestedAt)
        .ToArray();

    public async Task<SongRequestMutationResult> CreateAsync(
        string? artist,
        string? title,
        string tableId,
        string participantId,
        CancellationToken cancellationToken)
    {
        var normalizedArtist = Normalize(artist);
        var normalizedTitle = Normalize(title);
        if (normalizedArtist.Length is < 1 or > 100)
            return SongRequestMutationResult.Invalid("Artisten måste vara 1–100 tecken.");
        if (normalizedTitle.Length is < 1 or > 150)
            return SongRequestMutationResult.Invalid("Låten måste vara 1–150 tecken.");

        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            var songRequest = new SongRequest(
                Guid.NewGuid().ToString("N"),
                normalizedArtist,
                normalizedTitle,
                tableId,
                participantId,
                DateTimeOffset.UtcNow);
            await repository.SaveAsync(songRequest, cancellationToken);
            _requests[songRequest.Id] = songRequest;
            return SongRequestMutationResult.Success(songRequest);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            if (!_requests.ContainsKey(id)) return false;
            await repository.DeleteAsync(id, cancellationToken);
            _requests.TryRemove(id, out _);
            return true;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private static string Normalize(string? value) =>
        Whitespace().Replace(value?.Trim() ?? "", " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

public sealed record SongRequestMutationResult(SongRequest? SongRequest, string? Error)
{
    public static SongRequestMutationResult Success(SongRequest songRequest) => new(songRequest, null);
    public static SongRequestMutationResult Invalid(string error) => new(null, error);
}
