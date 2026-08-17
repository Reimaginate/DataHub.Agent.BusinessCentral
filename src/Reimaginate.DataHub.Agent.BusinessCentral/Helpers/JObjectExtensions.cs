using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Helpers;

public static class JObjectExtensions
{
    public static T ToObjectIgnoreErrors<T>(this JObject jObject)
    {
        var ser = new JsonSerializer();
        ser.Error += (_, args) =>
        {
            if (args.ErrorContext.Error.Message.StartsWith("Error reading"))
            {
                args.ErrorContext.Handled = true;
            }
        };

        return jObject.ToObject<T>(ser);
    }
}