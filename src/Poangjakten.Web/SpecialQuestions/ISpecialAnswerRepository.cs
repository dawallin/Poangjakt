namespace Poangjakten.Web.SpecialQuestions;

public interface ISpecialAnswerRepository
{
    Task<IReadOnlyList<SpecialAnswer>> LoadAllAsync(CancellationToken cancellationToken);
    Task SaveAsync(SpecialAnswer answer, CancellationToken cancellationToken);
    Task DeleteAsync(string participantId, string questionId, CancellationToken cancellationToken);
}
