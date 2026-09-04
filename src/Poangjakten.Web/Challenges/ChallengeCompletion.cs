namespace Poangjakten.Web.Challenges;

public sealed record ChallengeCompletion(
    string ParticipantId,
    string ChallengeId,
    DateTimeOffset CompletedAt);
