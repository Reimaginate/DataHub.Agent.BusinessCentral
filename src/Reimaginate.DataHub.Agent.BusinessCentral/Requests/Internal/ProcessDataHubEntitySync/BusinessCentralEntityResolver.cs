using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// ReSharper disable InconsistentNaming

namespace Reimaginate.DataHub.Agent.BusinessCentral.Requests.Internal.ProcessDataHubEntitySync;


public class BusinessCentralEntityResolver : DefaultContractResolver
{
    private readonly List<string> _includeProps;
    private readonly List<string> _ignoreProps;

    public BusinessCentralEntityResolver()
    {
        _ignoreProps = new();
        _includeProps = new();
    }


    public BusinessCentralEntityResolver(List<string>? includeProps = null, List<string>? ignoreProps = null)
    {
        _includeProps = includeProps ?? new();
        _ignoreProps = ignoreProps ?? new();
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, MemberSerialization.OptOut);
        prop.ShouldSerialize = o => !_includeProps.Any();

        if (_includeProps.Select(s => s.ToLower()).Contains(prop.PropertyName?.ToLower()))
        {
            prop.ShouldSerialize = o => true;
        }

        if (_ignoreProps.Select(s => s.ToLower()).Contains(prop.PropertyName?.ToLower()))
        {
            prop.ShouldSerialize = o => false;
        }

        return prop;
    }
}