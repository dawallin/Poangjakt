using Azure.Data.Tables;
using Poangjakten.Web.Storage;

namespace Poangjakten.Web.Participants;

public sealed class TableParticipantRepository(AzureStorageClients storage) : IParticipantRepository
{
    private const string PartitionKey = "participant";

    public async Task<IReadOnlyList<Participant>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var table = storage.PlayersTable();
        await table.CreateIfNotExistsAsync(cancellationToken);

        var participants = new List<Participant>();
        await foreach (var entity in table.QueryAsync<TableEntity>(
                           item => item.PartitionKey == PartitionKey,
                           cancellationToken: cancellationToken))
        {
            participants.Add(ToParticipant(entity));
        }

        return participants;
    }

    public async Task SaveAsync(Participant participant, CancellationToken cancellationToken)
    {
        var table = storage.PlayersTable();
        var entity = new TableEntity(PartitionKey, participant.Id)
        {
            ["DisplayName"] = participant.DisplayName,
            ["LoginCode"] = participant.LoginCode,
            ["Clue"] = participant.Clue,
            ["TableId"] = participant.TableId,
            ["Score"] = participant.Score,
            ["CreatedAt"] = participant.CreatedAt,
            ["UpdatedAt"] = participant.UpdatedAt
        };

        await table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task DeleteAsync(string participantId, CancellationToken cancellationToken)
    {
        var table = storage.PlayersTable();
        await table.DeleteEntityAsync(PartitionKey, participantId, cancellationToken: cancellationToken);
    }

    private static Participant ToParticipant(TableEntity entity) => new(
        entity.RowKey,
        entity.GetString("DisplayName") ?? "Okänd deltagare",
        entity.GetString("LoginCode") ?? "",
        entity.GetString("Clue") ?? "",
        entity.GetString("TableId") ?? "",
        entity.GetInt32("Score") ?? 0,
        entity.GetDateTimeOffset("CreatedAt") ?? entity.Timestamp ?? DateTimeOffset.UtcNow,
        entity.GetDateTimeOffset("UpdatedAt") ?? entity.Timestamp ?? DateTimeOffset.UtcNow);
}
