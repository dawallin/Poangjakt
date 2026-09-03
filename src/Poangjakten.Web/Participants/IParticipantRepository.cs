namespace Poangjakten.Web.Participants;

public interface IParticipantRepository
{
    Task<IReadOnlyList<Participant>> LoadAllAsync(CancellationToken cancellationToken);
    Task SaveAsync(Participant participant, CancellationToken cancellationToken);
    Task DeleteAsync(string participantId, CancellationToken cancellationToken);
}
