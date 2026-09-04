using Azure;
using Azure.Data.Tables;
using Poangjakten.Web.Storage;

namespace Poangjakten.Web.Challenges;

public sealed class TableChallengeCompletionRepository(AzureStorageClients storage)
    : IChallengeCompletionRepository
{
    public async Task<IReadOnlyList<ChallengeCompletion>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var table = storage.ChallengeCompletionsTable();
        await table.CreateIfNotExistsAsync(cancellationToken);

        var completions = new List<ChallengeCompletion>();
        await foreach (var entity in table.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
        {
            completions.Add(new ChallengeCompletion(
                entity.PartitionKey,
                entity.RowKey,
                entity.GetDateTimeOffset("CompletedAt") ?? entity.Timestamp ?? DateTimeOffset.UtcNow));
        }

        return completions;
    }

    public async Task SaveAsync(ChallengeCompletion completion, CancellationToken cancellationToken)
    {
        var entity = new TableEntity(completion.ParticipantId, completion.ChallengeId)
        {
            ["CompletedAt"] = completion.CompletedAt
        };
        await storage.ChallengeCompletionsTable()
            .UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task DeleteAsync(string participantId, string challengeId, CancellationToken cancellationToken)
    {
        try
        {
            await storage.ChallengeCompletionsTable()
                .DeleteEntityAsync(participantId, challengeId, ETag.All, cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
        }
    }

    public async Task DeleteAllForParticipantAsync(string participantId, CancellationToken cancellationToken)
    {
        var table = storage.ChallengeCompletionsTable();
        await foreach (var entity in table.QueryAsync<TableEntity>(
                           item => item.PartitionKey == participantId,
                           cancellationToken: cancellationToken))
        {
            await table.DeleteEntityAsync(participantId, entity.RowKey, ETag.All, cancellationToken);
        }
    }
}
