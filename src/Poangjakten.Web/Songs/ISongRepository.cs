namespace Poangjakten.Web.Songs;

public interface ISongRepository
{
    Task<IReadOnlyList<Song>> LoadAllAsync(CancellationToken cancellationToken);
    Task SaveAsync(Song song, CancellationToken cancellationToken);
    Task DeleteAsync(string songId, CancellationToken cancellationToken);
}
