using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Mapping;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral;
using Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub;
using Reimaginate.Mapper;
using BCSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.SalesOrder;
using BCSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral.SalesOrderLine;
using DHSalesOrder = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.SalesOrder;
using DHSalesOrderLine = Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.DataHub.SalesOrderLine;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Reference;

public static class ReferenceRegistration
{
    public static IServiceCollection AddBusinessCentralReference(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
        services.AddTransient<BusinessCentralBearerTokenHandler>();
        services.AddBusinessCentralAgent(options =>
            options.WithAppSettingsConfig(configuration, "BusinessCentralAgentOptions"));
        services.AddHttpClient("BusinessCentral")
            .AddHttpMessageHandler<BusinessCentralBearerTokenHandler>();

        services.AddTransient<ITypeMapper<Account, Customer>, MapAccountToCustomer>();
        services.AddTransient<ITypeMapper<Customer, Account>, MapCustomerToAccount>();
        services.AddTransient<ITypeMapper<Product, Item>, MapProductToItem>();
        services.AddTransient<ITypeMapper<Item, Product>, MapItemToProduct>();
        services.AddTransient<ITypeMapper<DHSalesOrder, BCSalesOrder>, MapSalesOrderToBusinessCentral>();
        services.AddTransient<ITypeMapper<BCSalesOrder, DHSalesOrder>, MapBusinessCentralToSalesOrder>();
        services.AddTransient<ITypeMapper<DHSalesOrderLine, BCSalesOrderLine>, MapSalesOrderLineToBusinessCentral>();
        services.AddTransient<ITypeMapper<BCSalesOrderLine, DHSalesOrderLine>, MapBusinessCentralToSalesOrderLine>();
        return services;
    }
}

public sealed class BusinessCentralBearerTokenHandler(TokenCredential credential) : DelegatingHandler
{
    private static readonly TokenRequestContext TokenContext =
        new(["https://api.businesscentral.dynamics.com/.default"]);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await credential.GetTokenAsync(TokenContext, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}

public static class ReferenceConfiguration
{
    public static IEnumerable<string> Validate(IConfiguration configuration)
    {
        var section = configuration.GetSection("BusinessCentralAgentOptions:BusinessCentralServiceOptions");
        var baseUrl = section["BaseUrl"];
        var companyId = section["CompanyId"];
        var apiRoute = section["ApiRoute"];

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            baseUrl!.Contains("YOUR-", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Set BusinessCentralAgentOptions:BusinessCentralServiceOptions:BaseUrl to the HTTPS Business Central API environment URL.";
        }
        if (!Guid.TryParse(companyId, out var parsedCompanyId) || parsedCompanyId == Guid.Empty)
        {
            yield return "Set BusinessCentralAgentOptions:BusinessCentralServiceOptions:CompanyId to the target company GUID.";
        }
        if (!string.Equals(apiRoute, "api/v2.0", StringComparison.OrdinalIgnoreCase))
        {
            yield return "The reference implementation expects the standard Business Central route api/v2.0.";
        }
    }
}
