using Azure.Data.Tables;
using Poangjakten.Web.Storage;

namespace Poangjakten.Web.PartyStages;

public sealed class TablePartyStageRepository(AzureStorageClients storage) : IPartyStageRepository
{
    private const string PartitionKey = "party-stage";

    public async Task<IReadOnlyList<PartyStageState>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var table = storage.AppStateTable();
        await table.CreateIfNotExistsAsync(cancellationToken);

        var stages = new List<PartyStageState>();
        await foreach (var entity in table.QueryAsync<TableEntity>(
                           item => item.PartitionKey == PartitionKey,
                           cancellationToken: cancellationToken))
        {
            stages.Add(new PartyStageState(
                entity.RowKey,
                entity.GetBoolean("IsUnlocked") ?? false,
                entity.GetDateTimeOffset("UpdatedAt") ?? entity.Timestamp ?? DateTimeOffset.UtcNow));
        }

        return stages;
    }

    public Task SaveAsync(PartyStageState stage, CancellationToken cancellationToken)
    {
        var entity = new TableEntity(PartitionKey, stage.Id)
        {
            ["IsUnlocked"] = stage.IsUnlocked,
            ["UpdatedAt"] = stage.UpdatedAt
        };

        return storage.AppStateTable().UpsertEntityAsync(
            entity,
            TableUpdateMode.Replace,
            cancellationToken);
    }
}
