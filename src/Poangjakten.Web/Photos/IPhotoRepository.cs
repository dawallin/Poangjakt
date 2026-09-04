namespace Poangjakten.Web.Photos;

public interface IPhotoRepository
{
    Task<IReadOnlyList<Photo>> LoadAllAsync(CancellationToken cancellationToken);
    Task SaveAsync(Photo photo, CancellationToken cancellationToken);
    Task DeleteAsync(string photoId, CancellationToken cancellationToken);
}
