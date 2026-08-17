using Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting.Containers;
using Xunit;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting.Xunit;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class BusinessCentralIntegrationTestCollection :
    ICollectionFixture<DataHubCosmosDbEmulator>,
    ICollectionFixture<DataHubRedisContainer>
{
    public const string Name = "Business Central Agent Integration Tests";
}
