using System.Collections.Concurrent;

namespace Poangjakten.Web.SpecialQuestions;

public sealed class SpecialAnswerRegistry(
    ISpecialAnswerRepository repository,
    ILogger<SpecialAnswerRegistry> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, SpecialAnswer> _answers = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _mutationLock = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var answers = await repository.LoadAllAsync(cancellationToken);
        foreach (var answer in answers)
        {
            _answers[Key(answer.ParticipantId, answer.QuestionId)] = answer;
        }

        logger.LogInformation("Loaded {SpecialAnswerCount} special answers into memory", answers.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public SpecialAnswer? Find(string participantId, string questionId) =>
        _answers.GetValueOrDefault(Key(participantId, questionId));

    public int GetPoints(string participantId, SpecialQuestionDefinition question)
    {
        var answer = Find(participantId, question.Id);
        return answer is null ? 0 : question.PointsFor(answer.Value);
    }

    public async Task<SpecialAnswerMutationResult> SetAsync(
        string participantId,
        string questionId,
        int value,
        CancellationToken cancellationToken)
    {
        var question = SpecialQuestionDefinitions.Find(questionId);
        if (question is null) return SpecialAnswerMutationResult.NotFound();
        if (value is < 0 or > 100)
            return SpecialAnswerMutationResult.Invalid("Svaret måste vara ett heltal mellan 0 och 100.");

        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            var answer = new SpecialAnswer(participantId, questionId, value, DateTimeOffset.UtcNow);
            await repository.SaveAsync(answer, cancellationToken);
            _answers[Key(participantId, questionId)] = answer;
            return SpecialAnswerMutationResult.Success(answer);
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public async Task<bool> RemoveAsync(
        string participantId,
        string questionId,
        CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken);
        try
        {
            var key = Key(participantId, questionId);
            if (!_answers.ContainsKey(key)) return false;
            await repository.DeleteAsync(participantId, questionId, cancellationToken);
            _answers.TryRemove(key, out _);
            return true;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private static string Key(string participantId, string questionId) =>
        $"{questionId}:{participantId}";
}

public sealed record SpecialAnswerMutationResult(SpecialAnswer? Answer, string? Error, bool WasNotFound)
{
    public static SpecialAnswerMutationResult Success(SpecialAnswer answer) => new(answer, null, false);
    public static SpecialAnswerMutationResult Invalid(string error) => new(null, error, false);
    public static SpecialAnswerMutationResult NotFound() => new(null, null, true);
}
