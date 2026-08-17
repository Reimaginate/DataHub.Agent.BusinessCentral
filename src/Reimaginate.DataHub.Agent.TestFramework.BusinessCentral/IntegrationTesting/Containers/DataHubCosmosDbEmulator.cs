using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Testcontainers.CosmosDb;
using Xunit;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting.Containers;

public sealed class DataHubCosmosDbEmulator : IAsyncLifetime
{
    private static readonly TimeSpan InitializationTimeout = TimeSpan.FromMinutes(3);
    private bool _reuseContainers;

    public static DataHubCosmosDbEmulator? Current { get; private set; }

    public DataHubCosmosDbEmulator()
    {
        Current = this;
    }

    public CosmosDbContainer? CosmosDbContainer { get; private set; }

    public string? ConnectionString { get; private set; }

    public string? SkipReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        var configuration = IntegrationConfiguration.Build();
        if (!configuration.GetValue<bool>($"{BusinessCentralIntegrationSettings.SectionName}:Enabled"))
        {
            return;
        }

        if (!configuration.GetValue<bool>("TestFixtures:UseLocalCosmosDb"))
        {
            SkipReason = "Business Central integration tests require TestFixtures:UseLocalCosmosDb=true so the Cosmos fixture provides a connection string.";
            return;
        }

        _reuseContainers = configuration.GetValue<bool>("TestFixtures:ReuseContainers");

        try
        {
            CosmosDbContainer = new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-latest")
                .WithCleanUp(true)
                .WithReuse(_reuseContainers)
                .Build();

            await CosmosDbContainer.StartAsync();
            ConnectionString = CosmosDbContainer.GetConnectionString();
            await CreateDatabaseAsync();
        }
        catch (Exception ex)
        {
            SkipReason = $"Business Central integration tests require Docker/Testcontainers with the Cosmos DB emulator available. {ex.Message}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_reuseContainers && CosmosDbContainer is not null)
        {
            await CosmosDbContainer.DisposeAsync();
        }
    }

    private async Task CreateDatabaseAsync()
    {
        ArgumentNullException.ThrowIfNull(CosmosDbContainer);

        using var client = new CosmosClient(
            CosmosDbContainer.GetConnectionString(),
            new CosmosClientOptions
            {
                HttpClientFactory = () => CosmosDbContainer.HttpClient,
                ConnectionMode = ConnectionMode.Gateway,
                AllowBulkExecution = true,
                RequestTimeout = TimeSpan.FromMinutes(2),
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
                MaxRetryAttemptsOnRateLimitedRequests = 10
            });

        await ExecuteWithRetryAsync(
            _ => client.ReadAccountAsync(),
            "connect to the Cosmos emulator");

        var database = await ExecuteWithRetryAsync(
            cancellationToken => client.CreateDatabaseIfNotExistsAsync("DataHub", cancellationToken: cancellationToken),
            "create the DataHub database");

        await EnsureContainerAsync(database.Database, "Entities", ["/entityType", "/id"]);
        await EnsureContainerAsync(database.Database, "TrackingData", ["/DataSource", "/EntityType", "/EntityId"]);
        await EnsureContainerAsync(database.Database, "SyncMarkers", ["/id"]);
        await EnsureContainerAsync(database.Database, "Logs", ["/Type", "/id"]);
        await EnsureContainerAsync(database.Database, "ResolutionPromises", ["/_id"]);
        await EnsureContainerAsync(database.Database, "Configs", ["/id"]);
        await EnsureContainerAsync(database.Database, "Management", ["/_dt", "/id"]);
    }

    private static Task<ContainerResponse> EnsureContainerAsync(
        Database database,
        string name,
        IReadOnlyList<string> partitionKeyPaths)
    {
        return ExecuteWithRetryAsync(
            cancellationToken => database.CreateContainerIfNotExistsAsync(
                new ContainerProperties { Id = name, PartitionKeyPaths = [.. partitionKeyPaths] },
                cancellationToken: cancellationToken),
            $"create the {name} container");
    }

    private static async Task<T> ExecuteWithRetryAsync<T>(Func<CancellationToken, Task<T>> operation, string description)
    {
        var deadline = DateTimeOffset.UtcNow.Add(InitializationTimeout);
        Exception? lastException = null;
        var attempt = 0;

        while (DateTimeOffset.UtcNow < deadline)
        {
            attempt++;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            try
            {
                return await operation(timeout.Token);
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(5 * attempt, 15)), CancellationToken.None);
            }
        }

        throw new TimeoutException($"Timed out waiting to {description}.", lastException);
    }

    private static bool IsTransient(Exception exception) => exception switch
    {
        CosmosException cosmosException when cosmosException.StatusCode is System.Net.HttpStatusCode.RequestTimeout
            or System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.TooManyRequests => true,
        TimeoutException or TaskCanceledException or OperationCanceledException or HttpRequestException or IOException => true,
        _ when exception.InnerException is not null => IsTransient(exception.InnerException),
        _ => false
    };
}
