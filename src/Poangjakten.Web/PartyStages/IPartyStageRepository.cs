namespace Poangjakten.Web.PartyStages;

public interface IPartyStageRepository
{
    Task<IReadOnlyList<PartyStageState>> LoadAllAsync(CancellationToken cancellationToken);
    Task SaveAsync(PartyStageState stage, CancellationToken cancellationToken);
}
