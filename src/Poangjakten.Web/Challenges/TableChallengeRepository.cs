using Azure.Data.Tables;
using Poangjakten.Web.Storage;

namespace Poangjakten.Web.Challenges;

public sealed class TableChallengeRepository(AzureStorageClients storage) : IChallengeRepository
{
    private const string PartitionKey = "challenge";

    public async Task<IReadOnlyList<Challenge>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var table = storage.ChallengesTable();
        await table.CreateIfNotExistsAsync(cancellationToken);

        var challenges = new List<Challenge>();
        await foreach (var entity in table.QueryAsync<TableEntity>(
                           item => item.PartitionKey == PartitionKey,
                           cancellationToken: cancellationToken))
        {
            challenges.Add(ToChallenge(entity));
        }

        return challenges;
    }

    public async Task SaveAsync(Challenge challenge, CancellationToken cancellationToken)
    {
        var entity = new TableEntity(PartitionKey, challenge.Id)
        {
            ["Description"] = challenge.Description,
            ["Points"] = challenge.Points,
            ["Scope"] = challenge.Scope,
            ["CreatedAt"] = challenge.CreatedAt,
            ["UpdatedAt"] = challenge.UpdatedAt
        };

        await storage.ChallengesTable().UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task DeleteAsync(string challengeId, CancellationToken cancellationToken)
    {
        await storage.ChallengesTable()
            .DeleteEntityAsync(PartitionKey, challengeId, cancellationToken: cancellationToken);
    }

    private static Challenge ToChallenge(TableEntity entity) => new(
        entity.RowKey,
        entity.GetString("Description") ?? "Uppgift utan beskrivning",
        entity.GetInt32("Points") ?? 0,
        ChallengeScopes.Normalize(entity.GetString("Scope")) ?? ChallengeScopes.Individual,
        entity.GetDateTimeOffset("CreatedAt") ?? entity.Timestamp ?? DateTimeOffset.UtcNow,
        entity.GetDateTimeOffset("UpdatedAt") ?? entity.Timestamp ?? DateTimeOffset.UtcNow);
}
