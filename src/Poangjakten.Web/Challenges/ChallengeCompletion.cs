namespace Poangjakten.Web.Challenges;

public sealed record ChallengeCompletion(
    string OwnerId,
    string ChallengeId,
    DateTimeOffset CompletedAt);

public static class ChallengeCompletionOwners
{
    public static string ForParticipant(string participantId) => participantId;
    public static string ForTable(string tableId) => $"table:{tableId}";
}
