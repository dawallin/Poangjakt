using Azure.Core;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace Poangjakten.Web.Storage;

public sealed class AzureStorageClients
{
    private readonly StorageOptions _options;
    private readonly TokenCredential _credential;

    public AzureStorageClients(IOptions<StorageOptions> options, TokenCredential credential)
    {
        _options = options.Value;
        _credential = credential;
    }

    public StorageOptions Options => _options;

    public TableClient PlayersTable()
    {
        EnsureConfigured(_options.PlayersTableName, nameof(_options.PlayersTableName));
        var service = new TableServiceClient(
            new Uri($"https://{AccountName()}.table.core.windows.net"),
            _credential);
        return service.GetTableClient(_options.PlayersTableName);
    }

    public TableClient ChallengesTable()
    {
        EnsureConfigured(_options.ChallengesTableName, nameof(_options.ChallengesTableName));
        var service = new TableServiceClient(
            new Uri($"https://{AccountName()}.table.core.windows.net"),
            _credential);
        return service.GetTableClient(_options.ChallengesTableName);
    }

    public BlobContainerClient PhotoContainer()
    {
        EnsureConfigured(_options.BlobContainerName, nameof(_options.BlobContainerName));
        return new BlobContainerClient(
            new Uri($"https://{AccountName()}.blob.core.windows.net/{_options.BlobContainerName}"),
            _credential);
    }

    private string AccountName()
    {
        EnsureConfigured(_options.AccountName, nameof(_options.AccountName));
        return _options.AccountName;
    }

    private static void EnsureConfigured(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Storage__{propertyName} saknas i App Service-konfigurationen.");
        }
    }
}
