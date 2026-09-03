using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace Poangjakten.Web.Storage;

public sealed class StorageDiagnostics(
    AzureStorageClients storage,
    ILogger<StorageDiagnostics> logger)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private StorageTestResult? _successfulResult;

    public async Task<StorageTestResult> RunAsync(CancellationToken cancellationToken)
    {
        if (_successfulResult is not null)
        {
            return _successfulResult with { Cached = true };
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_successfulResult is not null)
            {
                return _successfulResult with { Cached = true };
            }

            var tableTask = TestTableAsync(cancellationToken);
            var blobTask = TestBlobAsync(cancellationToken);
            await Task.WhenAll(tableTask, blobTask);

            var table = await tableTask;
            var blob = await blobTask;
            var result = new StorageTestResult(
                table.Success && blob.Success,
                table.Success,
                blob.Success,
                false,
                string.Join(" ", new[] { table.Error, blob.Error }.Where(value => value is not null)),
                DateTimeOffset.UtcNow);

            if (result.IsHealthy)
            {
                _successfulResult = result;
            }

            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<ComponentResult> TestTableAsync(CancellationToken cancellationToken)
    {
        var partitionKey = "storage-diagnostic";
        var rowKey = Guid.NewGuid().ToString("N");
        TableClient? table = null;

        try
        {
            table = storage.PlayersTable();
            await table.CreateIfNotExistsAsync(cancellationToken);

            var entity = new TableEntity(partitionKey, rowKey)
            {
                ["Message"] = "Poängjakten storage test",
                ["CreatedAt"] = DateTimeOffset.UtcNow
            };

            await table.AddEntityAsync(entity, cancellationToken);
            var stored = await table.GetEntityAsync<TableEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);

            if (stored.Value.GetString("Message") != "Poängjakten storage test")
            {
                throw new InvalidOperationException("Den lästa testposten hade oväntat innehåll.");
            }

            return ComponentResult.Ok;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Table Storage diagnostic failed");
            return new ComponentResult(false, $"Table Storage: {FriendlyMessage(exception)}");
        }
        finally
        {
            if (table is not null)
            {
                try
                {
                    await table.DeleteEntityAsync(partitionKey, rowKey, ETag.All, cancellationToken);
                }
                catch (RequestFailedException exception) when (exception.Status == 404)
                {
                }
            }
        }
    }

    private async Task<ComponentResult> TestBlobAsync(CancellationToken cancellationToken)
    {
        var blobName = $"diagnostics/{Guid.NewGuid():N}.txt";
        BlobClient? blob = null;

        try
        {
            var container = storage.PhotoContainer();
            await container.GetPropertiesAsync(cancellationToken: cancellationToken);

            blob = container.GetBlobClient(blobName);
            const string payload = "Poängjakten storage test";
            await blob.UploadAsync(BinaryData.FromString(payload), overwrite: true, cancellationToken);
            var download = await blob.DownloadContentAsync(cancellationToken);

            if (download.Value.Content.ToString() != payload)
            {
                throw new InvalidOperationException("Den nedladdade testblobben hade oväntat innehåll.");
            }

            return ComponentResult.Ok;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Blob Storage diagnostic failed");
            return new ComponentResult(false, $"Blob Storage: {FriendlyMessage(exception)}");
        }
        finally
        {
            if (blob is not null)
            {
                try
                {
                    await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                }
                catch (RequestFailedException exception) when (exception.Status == 404)
                {
                }
            }
        }
    }

    private static string FriendlyMessage(Exception exception) => exception switch
    {
        RequestFailedException requestFailed => $"Azure svarade {requestFailed.Status} ({requestFailed.ErrorCode ?? "okänd felkod"}).",
        _ => exception.Message
    };

    private sealed record ComponentResult(bool Success, string? Error)
    {
        public static readonly ComponentResult Ok = new(true, null);
    }
}

public sealed record StorageTestResult(
    bool IsHealthy,
    bool TableStorage,
    bool BlobStorage,
    bool Cached,
    string Error,
    DateTimeOffset CheckedAt);
