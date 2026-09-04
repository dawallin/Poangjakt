using Azure.Data.Tables;
using Poangjakten.Web.Storage;

namespace Poangjakten.Web.Songs;

public sealed class TableSongRepository(AzureStorageClients storage) : ISongRepository
{
    private const string PartitionKey = "song";

    public async Task<IReadOnlyList<Song>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var table = storage.SongsTable();
        await table.CreateIfNotExistsAsync(cancellationToken);

        var songs = new List<Song>();
        await foreach (var entity in table.QueryAsync<TableEntity>(
                           item => item.PartitionKey == PartitionKey,
                           cancellationToken: cancellationToken))
        {
            songs.Add(ToSong(entity));
        }

        return songs;
    }

    public async Task SaveAsync(Song song, CancellationToken cancellationToken)
    {
        var entity = new TableEntity(PartitionKey, song.Id)
        {
            ["Title"] = song.Title,
            ["Melody"] = song.Melody,
            ["Lyrics"] = song.Lyrics,
            ["SortOrder"] = song.SortOrder,
            ["CreatedAt"] = song.CreatedAt,
            ["UpdatedAt"] = song.UpdatedAt
        };
        if (song.ImageBlobName is not null)
        {
            entity["ImageBlobName"] = song.ImageBlobName;
        }

        await storage.SongsTable().UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task DeleteAsync(string songId, CancellationToken cancellationToken)
    {
        await storage.SongsTable()
            .DeleteEntityAsync(PartitionKey, songId, cancellationToken: cancellationToken);
    }

    private static Song ToSong(TableEntity entity) => new(
        entity.RowKey,
        entity.GetString("Title") ?? "Sång utan titel",
        entity.GetString("Melody") ?? "",
        entity.GetString("Lyrics") ?? "",
        entity.GetInt32("SortOrder") ?? 0,
        entity.GetString("ImageBlobName"),
        entity.GetDateTimeOffset("CreatedAt") ?? entity.Timestamp ?? DateTimeOffset.UtcNow,
        entity.GetDateTimeOffset("UpdatedAt") ?? entity.Timestamp ?? DateTimeOffset.UtcNow);
}
