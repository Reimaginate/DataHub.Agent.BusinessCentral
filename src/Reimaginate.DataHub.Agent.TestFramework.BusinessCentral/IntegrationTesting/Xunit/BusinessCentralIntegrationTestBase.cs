using Microsoft.Extensions.Configuration;
using Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.Mediator;
using Reimaginate.Test.Framework;
using Xunit;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting.Xunit;

[Collection(BusinessCentralIntegrationTestCollection.Name)]
public abstract class BusinessCentralIntegrationTestBase : IDisposable
{
    protected BusinessCentralIntegrationTestBase()
    {
        Host = BusinessCentralIntegrationTestHost.Create(GetType());
    }

    protected BusinessCentralIntegrationTestHost Host { get; }

    public IConfigurationRoot Configuration => Host.Configuration;

    public IServiceProvider ServiceProvider => Host.ServiceProvider;

    protected string TestInstanceId => Host.TestInstanceId;

    protected string TestPrefix => Host.TestPrefix;

    protected IMediator Mediator => Host.Mediator;

    protected IDataHubClient DataHubClient => Host.DataHubClient;

    protected IBusinessCentralODataService BusinessCentralService => Host.BusinessCentralService;

    protected string? TestDisplayName([System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
    {
        var memberInfo = GetType().GetMember(memberName).FirstOrDefault();
        var factAttribute = memberInfo?.GetCustomAttributes(typeof(FactAttribute), true).OfType<FactAttribute>().FirstOrDefault();
        return factAttribute?.DisplayName;
    }

    protected static Task<ScenarioActionResult> ActionResult(object? currentObject, Dictionary<string, object?> stash)
    {
        return Task.FromResult(new ScenarioActionResult { CurrentObject = currentObject, Outputs = stash });
    }

    public void Dispose()
    {
        Host.Dispose();
    }
}
