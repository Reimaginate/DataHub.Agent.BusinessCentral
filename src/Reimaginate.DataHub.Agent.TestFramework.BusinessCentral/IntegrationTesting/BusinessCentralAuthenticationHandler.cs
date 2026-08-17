using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting;

public sealed class BusinessCentralAuthenticationHandler : DelegatingHandler
{
    private static readonly TokenRequestContext TokenContext =
        new(["https://api.businesscentral.dynamics.com/.default"]);

    private readonly ClientSecretCredential _credential;

    public BusinessCentralAuthenticationHandler(IOptions<BusinessCentralIntegrationSettings> options)
    {
        var settings = options.Value;
        _credential = new ClientSecretCredential(settings.TenantId, settings.ClientId, settings.ClientSecret);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _credential.GetTokenAsync(TokenContext, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}
