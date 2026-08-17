using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;
using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral;
using Reimaginate.DataHub.Client;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;

namespace Reimaginate.DataHub.Agent.BusinessCentral.UnitTests.Framework;

public class BusinessCentralAgentFrameworkTests
{
    [Fact(DisplayName = "Business Central agent options default to BusinessCentral data source")]
    [Trait("Category", "Unit")]
    public void BusinessCentralAgentOptionsDefaultToBusinessCentralDataSource()
    {
        var options = new BusinessCentralAgentOptions();

        options.DataSource.Should().Be("BusinessCentral");
        options.ProcessingLockOptions.UseRepository.Should().Be("InMemory");
    }

    [Fact(DisplayName = "AddBusinessCentralAgent binds config and supports explicit data source override")]
    [Trait("Category", "Unit")]
    public void AddBusinessCentralAgentBindsConfigAndSupportsExplicitDataSourceOverride()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AgentId"] = "BusinessCentralTestAgent",
                ["DataSource"] = "ConfiguredSource",
                ["ProcessingLockOptions:UseRepository"] = "inmemory",
                ["BusinessCentralServiceOptions:BaseUrl"] = "https://businesscentral.example/",
                ["BusinessCentralServiceOptions:CompanyId"] = "company-1",
                ["BusinessCentralServiceOptions:ApiRoute"] = "api/v2.0"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddBusinessCentralAgent(options => options
            .WithAppSettingsConfig(config)
            .WithDataSource("BusinessCentralOverride"));

        using var provider = services.BuildServiceProvider();
        var agentOptions = provider.GetRequiredService<IOptions<BusinessCentralAgentOptions>>().Value;
        var serviceOptions = provider.GetRequiredService<IOptions<BusinessCentralServiceOptions>>().Value;

        agentOptions.AgentId.Should().Be("BusinessCentralTestAgent");
        agentOptions.DataSource.Should().Be("BusinessCentralOverride");
        agentOptions.ProcessingLockOptions.UseRepository.Should().Be("inmemory");
        serviceOptions.BaseUrl.Should().Be("https://businesscentral.example/");
        serviceOptions.CompanyId.Should().Be("company-1");
        serviceOptions.ApiRoute.Should().Be("api/v2.0");
    }

    [Fact(DisplayName = "Business Central document stores source id and attributes")]
    [Trait("Category", "Unit")]
    public void BusinessCentralDocumentStoresSourceIdAndAttributes()
    {
        var document = new TestBusinessCentralDocument { Id = "bc-1", Number = "SO-1" };

        document.Id.Should().Be("bc-1");
        document.Number.Should().Be("SO-1");
        document.GetAttributes().Should().Contain("number", "SO-1");
    }

    [Fact(DisplayName = "Business Central test DSL creates merge and sync requests")]
    [Trait("Category", "Unit")]
    public void BusinessCentralTestDslCreatesMergeAndSyncRequests()
    {
        var agent = new BusinessCentralAgent("businesscentral-tests");

        var merge = agent.MergeSpecific<TestBusinessCentralDocument, TestDataHubEntity>(new[] { "bc-1" }, forceUpdate: true);
        var sync = agent.SyncSpecific<TestDataHubEntity, TestBusinessCentralDocument>(new[] { "dh-1" });

        merge.CorrelationId.Should().Be("businesscentral-tests");
        merge.EntityIds.Should().ContainSingle().Which.Should().Be("bc-1");
        merge.ForceUpdate.Should().BeTrue();
        sync.CorrelationId.Should().Be("businesscentral-tests");
        sync.EntityIds.Should().ContainSingle().Which.Should().Be("dh-1");
    }

    private sealed class TestBusinessCentralDocument : BusinessCentralDocument
    {
        public string? Number
        {
            get => GetAttributeValue<string>("number");
            set => SetWithNotification("number", value!);
        }
    }

    [Fact(DisplayName = "AddBusinessCentralAgent keeps root DataHub client configuration when agent settings use a section")]
    [Trait("Category", "Unit")]
    public void AddBusinessCentralAgentKeepsRootDataHubClientConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BusinessCentralAgentOptions:AgentId"] = "BusinessCentralTestAgent",
                ["BusinessCentralAgentOptions:BusinessCentralServiceOptions:BaseUrl"] = "https://businesscentral.example/",
                ["DataHubClientOptions:AuthenticationMode"] = "SharedKey",
                ["DataHubClientOptions:DataHubClientUrl"] = "https://datahub.example/api/Client",
                ["DataHubClientOptions:Key"] = "test-only-key"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddBusinessCentralAgent(options =>
            options.WithAppSettingsConfig(config, "BusinessCentralAgentOptions"));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IDataHubClient>().Should().NotBeNull();
    }

    private sealed class TestDataHubEntity : DataHubEntity
    {
    }
}
