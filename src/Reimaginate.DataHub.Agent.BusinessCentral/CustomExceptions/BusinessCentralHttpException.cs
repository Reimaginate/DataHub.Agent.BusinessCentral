using System.Net;

namespace Reimaginate.DataHub.Agent.BusinessCentral.CustomExceptions;

public sealed class BusinessCentralHttpException : HttpRequestException
{
    public BusinessCentralHttpException(HttpStatusCode statusCode, string operation, string? responseBody)
        : base(BuildMessage(statusCode, operation, responseBody), null, statusCode)
    {
        Operation = operation;
        ResponseBody = responseBody;
    }

    public string Operation { get; }
    public string? ResponseBody { get; }

    private static string BuildMessage(HttpStatusCode statusCode, string operation, string? responseBody)
    {
        var category = statusCode switch
        {
            HttpStatusCode.Unauthorized => "authentication",
            HttpStatusCode.Forbidden => "authorization",
            HttpStatusCode.NotFound => "environment, company, or record",
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => "validation",
            HttpStatusCode.PreconditionFailed => "concurrency",
            _ => "HTTP"
        };

        return $"Business Central {category} failure during {operation}: {(int)statusCode} ({statusCode})"
            + (string.IsNullOrWhiteSpace(responseBody) ? "." : $". {responseBody}");
    }
}
