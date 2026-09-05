using Azure.Data.Tables;
using Poangjakten.Web.Storage;

namespace Poangjakten.Web.SongRequests;

public sealed class TableSongRequestRepository(AzureStorageClients storage) : ISongRequestRepository
{
    private const string PartitionKey = "song-request";

    public async Task<IReadOnlyList<SongRequest>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var table = storage.SongRequestsTable();
        await table.CreateIfNotExistsAsync(cancellationToken);

        var requests = new List<SongRequest>();
        await foreach (var entity in table.QueryAsync<TableEntity>(
                           item => item.PartitionKey == PartitionKey,
                           cancellationToken: cancellationToken))
        {
            requests.Add(ToSongRequest(entity));
        }

        return requests;
    }

    public async Task SaveAsync(SongRequest songRequest, CancellationToken cancellationToken)
    {
        var entity = new TableEntity(PartitionKey, songRequest.Id)
        {
            ["Artist"] = songRequest.Artist,
            ["Title"] = songRequest.Title,
            ["TableId"] = songRequest.TableId,
            ["RequestedByParticipantId"] = songRequest.RequestedByParticipantId,
            ["RequestedAt"] = songRequest.RequestedAt
        };

        await storage.SongRequestsTable()
            .UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task DeleteAsync(string songRequestId, CancellationToken cancellationToken)
    {
        await storage.SongRequestsTable()
            .DeleteEntityAsync(PartitionKey, songRequestId, cancellationToken: cancellationToken);
    }

    private static SongRequest ToSongRequest(TableEntity entity) => new(
        entity.RowKey,
        entity.GetString("Artist") ?? "Okänd artist",
        entity.GetString("Title") ?? "Namnlös låt",
        entity.GetString("TableId") ?? "",
        entity.GetString("RequestedByParticipantId") ?? "",
        entity.GetDateTimeOffset("RequestedAt") ?? entity.Timestamp ?? DateTimeOffset.UtcNow);
}
