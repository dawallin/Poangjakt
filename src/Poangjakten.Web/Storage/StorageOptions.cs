namespace Poangjakten.Web.Storage;

public sealed class StorageOptions
{
    public string AccountName { get; init; } = "";
    public string BlobContainerName { get; init; } = "";
    public string PlayersTableName { get; init; } = "players";
    public string ChallengesTableName { get; init; } = "challenges";
    public string ChallengeCompletionsTableName { get; init; } = "challengecompletions";
    public string PhotosTableName { get; init; } = "photos";
    public string SongsTableName { get; init; } = "songs";
    public string SongRequestsTableName { get; init; } = "songrequests";
    public string SpecialAnswersTableName { get; init; } = "specialanswers";
    public string AppStateTableName { get; init; } = "appstate";
}
