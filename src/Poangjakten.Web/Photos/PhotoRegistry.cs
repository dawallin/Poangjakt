using System.Collections.Concurrent;

namespace Poangjakten.Web.Photos;

public sealed class PhotoRegistry(
    IPhotoRepository repository,
    ILogger<PhotoRegistry> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, Photo> _photos = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var photos = await repository.LoadAllAsync(cancellationToken);
        foreach (var photo in photos)
        {
            _photos[photo.Id] = photo;
        }

        logger.LogInformation("Loaded {PhotoCount} photo records into memory", photos.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Photo? Find(string id) => _photos.GetValueOrDefault(id);

    public IReadOnlyList<Photo> List() => _photos.Values
        .OrderByDescending(photo => photo.UploadedAt)
        .ToArray();

    public async Task AddAsync(Photo photo, CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            await repository.SaveAsync(photo, cancellationToken);
            _photos[photo.Id] = photo;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async Task<Photo?> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            if (!_photos.TryGetValue(id, out var photo))
            {
                return null;
            }

            await repository.DeleteAsync(id, cancellationToken);
            _photos.TryRemove(id, out _);
            return photo;
        }
        finally
        {
            _mutationLock.Release();
        }
    }
}
