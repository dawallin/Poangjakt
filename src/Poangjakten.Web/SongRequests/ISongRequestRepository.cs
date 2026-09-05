namespace Poangjakten.Web.SongRequests;

public interface ISongRequestRepository
{
    Task<IReadOnlyList<SongRequest>> LoadAllAsync(CancellationToken cancellationToken);
    Task SaveAsync(SongRequest songRequest, CancellationToken cancellationToken);
    Task DeleteAsync(string songRequestId, CancellationToken cancellationToken);
}
