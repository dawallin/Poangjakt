using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Poangjakten.Web.Songs;

public sealed partial class SongRegistry(
    ISongRepository repository,
    ILogger<SongRegistry> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, Song> _songs = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var songs = await repository.LoadAllAsync(cancellationToken);
        foreach (var song in songs)
        {
            _songs[song.Id] = song;
        }

        logger.LogInformation("Loaded {SongCount} songs into memory", songs.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Song? Find(string id) => _songs.GetValueOrDefault(id);

    public IReadOnlyList<Song> List() => _songs.Values
        .OrderBy(song => song.SortOrder)
        .ThenBy(song => song.Title, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    public async Task<SongMutationResult> CreateAsync(
        string? title,
        string? melody,
        string? lyrics,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var validation = Validate(title, melody, lyrics, sortOrder);
        if (validation.Error is not null)
        {
            return SongMutationResult.Invalid(validation.Error);
        }

        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var song = new Song(
                Guid.NewGuid().ToString("N"),
                validation.Title!,
                validation.Melody!,
                validation.Lyrics!,
                sortOrder,
                null,
                now,
                now);
            await repository.SaveAsync(song, cancellationToken);
            _songs[song.Id] = song;
            return SongMutationResult.Success(song);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async Task<SongMutationResult> UpdateAsync(
        string id,
        string? title,
        string? melody,
        string? lyrics,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var validation = Validate(title, melody, lyrics, sortOrder);
        if (validation.Error is not null)
        {
            return SongMutationResult.Invalid(validation.Error);
        }

        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            if (!_songs.TryGetValue(id, out var existing))
            {
                return SongMutationResult.NotFound();
            }

            var updated = existing with
            {
                Title = validation.Title!,
                Melody = validation.Melody!,
                Lyrics = validation.Lyrics!,
                SortOrder = sortOrder,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await repository.SaveAsync(updated, cancellationToken);
            _songs[id] = updated;
            return SongMutationResult.Success(updated);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async Task<Song?> SetImageAsync(
        string id,
        string? imageBlobName,
        CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            if (!_songs.TryGetValue(id, out var existing))
            {
                return null;
            }

            var updated = existing with
            {
                ImageBlobName = imageBlobName,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await repository.SaveAsync(updated, cancellationToken);
            _songs[id] = updated;
            return updated;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async Task<Song?> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            if (!_songs.TryGetValue(id, out var existing))
            {
                return null;
            }

            await repository.DeleteAsync(id, cancellationToken);
            _songs.TryRemove(id, out _);
            return existing;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private static SongValidation Validate(string? title, string? melody, string? lyrics, int sortOrder)
    {
        var normalizedTitle = NormalizeSingleLine(title);
        var normalizedMelody = NormalizeSingleLine(melody);
        var normalizedLyrics = lyrics?.Trim().Replace("\r\n", "\n") ?? "";

        if (normalizedTitle.Length is < 2 or > 100)
        {
            return SongValidation.Invalid("Titeln måste vara 2–100 tecken.");
        }

        if (normalizedMelody.Length > 150)
        {
            return SongValidation.Invalid("Melodin får vara högst 150 tecken.");
        }

        if (normalizedLyrics.Length is < 1 or > 12000)
        {
            return SongValidation.Invalid("Sångtexten måste vara 1–12 000 tecken.");
        }

        if (sortOrder is < 0 or > 10000)
        {
            return SongValidation.Invalid("Ordningen måste vara mellan 0 och 10 000.");
        }

        return new SongValidation(normalizedTitle, normalizedMelody, normalizedLyrics, null);
    }

    private static string NormalizeSingleLine(string? value) =>
        Whitespace().Replace(value?.Trim() ?? "", " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    private sealed record SongValidation(string? Title, string? Melody, string? Lyrics, string? Error)
    {
        public static SongValidation Invalid(string error) => new(null, null, null, error);
    }
}

public sealed record SongMutationResult(Song? Song, string? Error, bool WasNotFound)
{
    public static SongMutationResult Success(Song song) => new(song, null, false);
    public static SongMutationResult Invalid(string error) => new(null, error, false);
    public static SongMutationResult NotFound() => new(null, null, true);
}
