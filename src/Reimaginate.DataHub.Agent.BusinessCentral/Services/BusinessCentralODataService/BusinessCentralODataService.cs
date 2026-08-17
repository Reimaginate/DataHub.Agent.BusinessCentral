// ReSharper disable InconsistentNaming

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.Options;
using OneOf;
using Reimaginate.DataHub.Agent.BusinessCentral.AppSettings;
using Reimaginate.DataHub.Agent.BusinessCentral.CustomExceptions;
using Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.CreateBusinessCentralRecords;
using Reimaginate.DataHub.Agent.BusinessCentral.DataAccess.Commands.UpdateBusinessCentralRecords;
using Reimaginate.DataHub.Agent.BusinessCentral.Contracts;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Services.BusinessCentralODataService;

public interface IBusinessCentralODataService
{
    Task<OneOf<CreateResult<T>, Exception>> CreateEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BusinessCentralDocument;
    Task<OneOf<List<CreateResult<T>>, Exception>> CreateEntitiesAsync<T>(List<T> entities, CancellationToken cancellationToken = default) where T : BusinessCentralDocument;
    Task<OneOf<bool, HttpResponseMessage, Exception>> DeleteEntityAsync<T>(string entityId, CancellationToken cancellationToken = default) where T : BusinessCentralDocument;
    Task<OneOf<bool, HttpResponseMessage, Exception>> DeleteEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BusinessCentralDocument;
    Task<OneOf<T, HttpResponseMessage, Exception>> GetEntityAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BusinessCentralDocument;
    Task<OneOf<T, HttpResponseMessage, Exception>> GetEntityAsync<T>(Guid parentId, Guid id, CancellationToken cancellationToken = default) where T : BusinessCentralDocument;
    Task<OneOf<ApiCollectionResponse<T>, HttpResponseMessage, Exception>> GetEntitiesAsync<T>(string? filter = null, int? skip = null, int? top = null, string? order = null, string? select = null, CancellationToken cancellationToken = default) where T : BusinessCentralDocument;
    Task<OneOf<ApiCollectionResponse<T>, HttpResponseMessage, Exception>> GetEntitiesAsync<T>(Guid parentId, string? filter = null, int? skip = null, int? top = null, string? order = null, string? select = null, CancellationToken cancellationToken = default) where T : BusinessCentralDocument;
    Task<UpdateResult<T>> UpdateEntityAsync<T>(T entity, CancellationToken cancellationToken) where T : BusinessCentralDocument;
    Task<List<UpdateResult<T>>> UpdateEntitiesAsync<T>(List<T> entities, CancellationToken cancellationToken) where T : BusinessCentralDocument;
}

public class BusinessCentralODataService : IBusinessCentralODataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<BusinessCentralServiceOptions> _config;
    private readonly JsonSerializerOptions _defaultSerializerOptions;

    public BusinessCentralODataService(IHttpClientFactory httpClientFactory, IOptions<BusinessCentralServiceOptions> config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _defaultSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters =
            {
                new EmptyStringJsonConverter()
            }
        };
    }

    public async Task<OneOf<CreateResult<T>, Exception>> CreateEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BusinessCentralDocument
    {
        var response = await CreateEntitiesAsync([entity], cancellationToken);
        if (response.IsT1) return response.AsT1;
        return response.AsT0.Single();
    }

    public async Task<OneOf<List<CreateResult<T>>, Exception>> CreateEntitiesAsync<T>(List<T> entities, CancellationToken cancellationToken = default) where T : BusinessCentralDocument
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("BusinessCentral");

            var results = new List<CreateResult<T>>();

            foreach (var entity in entities)
            {
                var reservationResult = await TryCreateUsingCorrelationReservationAsync(entity, cancellationToken);
                if (reservationResult is not null)
                {
                    results.Add(reservationResult);
                    continue;
                }

                var url = GetMutationCollectionUrl(entity);
                var json = JsonSerializer.Serialize(entity, _defaultSerializerOptions);
                var recoveryKey = GetCreateRecoveryKey(entity);
                var maximumAttempts = Math.Max(1, _config.Value.MaxRetryAttempts);

                for (var attempt = 1; attempt <= maximumAttempts; attempt++)
                {
                    HttpResponseMessage? response = null;
                    try
                    {
                        using var body = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        response = await httpClient.PostAsync(url, body, cancellationToken);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                            T? result = null;
                            Exception? malformedResponse = null;
                            try
                            {
                                if (string.IsNullOrWhiteSpace(responseBody))
                                {
                                    throw new InvalidDataException(
                                        "Business Central returned an empty successful create response.");
                                }

                                result = JsonSerializer.Deserialize<T>(responseBody, _defaultSerializerOptions);
                                if (result is null)
                                {
                                    throw new InvalidDataException(
                                        "Business Central returned a null successful create response.");
                                }
                                if (string.IsNullOrWhiteSpace(result.Id))
                                {
                                    throw new InvalidDataException(
                                        "Business Central returned an incomplete successful create response " +
                                        "without a record id.");
                                }
                            }
                            catch (Exception exception) when (
                                exception is JsonException or InvalidDataException or NotSupportedException)
                            {
                                malformedResponse = exception;
                            }

                            if (malformedResponse is null)
                            {
                                results.Add(new CreateResult<T>
                                {
                                    Success = true,
                                    EntityId = result!.Id!,
                                    ResultingEntity = result
                                });
                                break;
                            }

                            if (recoveryKey is not null)
                            {
                                var recovered = await TryFindCreatedEntityAsync<T>(
                                    recoveryKey,
                                    cancellationToken);
                                if (recovered is not null)
                                {
                                    results.Add(new CreateResult<T>
                                    {
                                        Success = true,
                                        EntityId = recovered.Id!,
                                        ResultingEntity = recovered
                                    });
                                    break;
                                }
                            }

                            var recoveryDescription = recoveryKey is null
                                ? "the entity has no deterministic recovery key"
                                : "the deterministic recovery lookup did not resolve exactly one matching record";
                            results.Add(FailedCreate<T>(entity, new InvalidOperationException(
                                $"Business Central returned {(int)response.StatusCode} for the {typeof(T).Name} " +
                                $"create, but its successful response was null, malformed, incomplete, or missing " +
                                $"a record id. The create outcome is ambiguous and the POST was not retried because " +
                                $"it may already have committed; retrying could create a duplicate. Since " +
                                $"{recoveryDescription}, reconcile the record in Business Central before retrying.",
                                malformedResponse)));
                            break;
                        }

                        var errorDescription = await response.Content.ReadAsStringAsync(cancellationToken);
                        var mayAlreadyExist = IndicatesEntityAlreadyExists(response.StatusCode, errorDescription) ||
                            IsTransient(response.StatusCode);
                        if (mayAlreadyExist && recoveryKey is not null)
                        {
                            var recovered = await TryFindCreatedEntityAsync<T>(recoveryKey, cancellationToken);
                            if (recovered is not null)
                            {
                                results.Add(new CreateResult<T>
                                {
                                    Success = true,
                                    EntityId = recovered.Id!,
                                    ResultingEntity = recovered
                                });
                                break;
                            }
                        }

                        if (IsAmbiguousCreateResponse(response.StatusCode))
                        {
                            var recoveryDescription = recoveryKey is null
                                ? "this entity has no deterministic recovery key"
                                : "its recovery lookup did not resolve exactly one matching record";
                            results.Add(FailedCreate<T>(entity, new InvalidOperationException(
                                $"Business Central returned {(int)response.StatusCode} while creating " +
                                $"{typeof(T).Name}. The create was not retried because {recoveryDescription}; " +
                                "retrying an ambiguous create could create a duplicate.")));
                            break;
                        }

                        if (IsTransient(response.StatusCode) && attempt < maximumAttempts)
                        {
                            var delay = GetRetryDelay(response, attempt);
                            response.Dispose();
                            response = null;
                            await Task.Delay(delay, cancellationToken);
                            continue;
                        }

                        results.Add(FailedCreate<T>(entity,
                            new BusinessCentralHttpException(response.StatusCode, $"create {typeof(T).Name}", errorDescription)));
                        break;
                    }
                    catch (Exception ex) when (IsTransient(ex, cancellationToken))
                    {
                        if (recoveryKey is not null)
                        {
                            var recovered = await TryFindCreatedEntityAsync<T>(recoveryKey, cancellationToken);
                            if (recovered is not null)
                            {
                                results.Add(new CreateResult<T>
                                {
                                    Success = true,
                                    EntityId = recovered.Id!,
                                    ResultingEntity = recovered
                                });
                                break;
                            }
                        }

                        var recoveryDescription = recoveryKey is null
                            ? "this entity has no deterministic recovery key"
                            : "its recovery lookup did not resolve exactly one matching record";
                        results.Add(FailedCreate<T>(entity, new InvalidOperationException(
                            $"Business Central create for {typeof(T).Name} ended with an ambiguous " +
                            $"transport failure. It was not retried because {recoveryDescription}; retrying " +
                            "an ambiguous create could create a duplicate.",
                            ex)));
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (recoveryKey is not null)
                        {
                            var recovered = await TryFindCreatedEntityAsync<T>(recoveryKey, cancellationToken);
                            if (recovered is not null)
                            {
                                results.Add(new CreateResult<T>
                                {
                                    Success = true,
                                    EntityId = recovered.Id!,
                                    ResultingEntity = recovered
                                });
                                break;
                            }
                        }

                        results.Add(FailedCreate<T>(entity, ex));
                        break;
                    }
                    finally
                    {
                        response?.Dispose();
                    }
                }

                if (results.Count < entities.IndexOf(entity) + 1)
                {
                    results.Add(FailedCreate<T>(entity,
                        new TimeoutException($"Business Central create did not complete after {maximumAttempts} attempts.")));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private async Task<CreateResult<T>?> TryCreateUsingCorrelationReservationAsync<T>(
        T entity,
        CancellationToken cancellationToken)
        where T : BusinessCentralDocument
    {
        var reservation = typeof(T).GetCustomAttribute<BusinessCentralCreateReservationAttribute>(inherit: true);
        if (!_config.Value.CorrelationReservationsEnabled || reservation is null)
            return null;

        try
        {
            var fields = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => (Property: property, Attribute: property.GetCustomAttribute<BusinessCentralReservationFieldAttribute>()))
                .Where(item => item.Attribute is not null)
                .ToArray();
            var correlationField = fields.FirstOrDefault(item =>
                item.Attribute!.Name.Equals("correlationId", StringComparison.OrdinalIgnoreCase));
            var correlationValue = correlationField.Property?.GetValue(entity);
            if (correlationValue is not Guid correlationId || correlationId == Guid.Empty)
                return FailedCreate<T>(entity, new InvalidOperationException(
                    $"Business Central correlation reservation for {typeof(T).Name} requires a non-empty correlationId."));

            var payload = new Dictionary<string, object?>
            {
                ["documentType"] = reservation.DocumentType
            };
            foreach (var field in fields)
            {
                var value = field.Property.GetValue(entity);
                if (value is null || value is Guid guid && guid == Guid.Empty)
                    return FailedCreate<T>(entity, new InvalidOperationException(
                        $"Business Central correlation reservation for {typeof(T).Name} requires '{field.Attribute!.Name}'."));
                payload[field.Attribute!.Name] = value;
            }

            var httpClient = _httpClientFactory.CreateClient("BusinessCentral");
            var reservationUrl = GetCorrelationReservationCollectionUrl(reservation.EntitySet);
            Guid? reservedId = null;
            string? errorBody = null;
            HttpStatusCode? statusCode = null;
            Exception? transportException = null;
            try
            {
                using var body = new StringContent(
                    JsonSerializer.Serialize(payload, _defaultSerializerOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");
                using var response = await httpClient.PostAsync(reservationUrl, body, cancellationToken);
                statusCode = response.StatusCode;
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.IsSuccessStatusCode)
                    reservedId = TryReadEntityId(responseBody);
                else
                    errorBody = responseBody;
            }
            catch (Exception exception) when (IsTransient(exception, cancellationToken))
            {
                transportException = exception;
            }

            reservedId ??= await TryFindReservationAsync(httpClient, reservationUrl, correlationId, cancellationToken);
            if (!reservedId.HasValue)
            {
                var message = transportException is not null
                    ? $"Business Central correlation reservation for {typeof(T).Name} ended with an ambiguous transport failure. " +
                      "The unique correlation lookup found no committed reservation; retrying the same Data Hub entity is safe and will reuse the same correlation id."
                    : $"Business Central correlation reservation for {typeof(T).Name} returned {(int?)statusCode ?? 0}. " +
                      "The unique correlation lookup found no committed reservation.";
                return FailedCreate<T>(entity, new InvalidOperationException(message +
                    (string.IsNullOrWhiteSpace(errorBody) ? string.Empty : $" Response: {errorBody}"), transportException));
            }

            var current = await GetReservedStandardEntityAsync<T>(entity, reservedId.Value, cancellationToken);
            if (current.IsT2)
                return FailedCreate<T>(entity, current.AsT2);
            if (current.IsT1)
            {
                using var response = current.AsT1;
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return FailedCreate<T>(entity, new BusinessCentralHttpException(
                    response.StatusCode, $"read reserved {typeof(T).Name}", body));
            }

            entity.Id = current.AsT0.Id;
            entity.ETag = current.AsT0.ETag;
            var updated = await UpdateEntityAsync(entity, cancellationToken);
            if (!updated.Success || updated.ResultingEntity is null)
                return FailedCreate<T>(entity, new InvalidOperationException(
                    $"Business Central reserved {typeof(T).Name} {reservedId}, but applying its mapped fields failed. " +
                    "Retry the same Data Hub entity to reuse the reservation; do not create another record.",
                    updated.Exception));

            return new CreateResult<T>
            {
                Success = true,
                EntityId = updated.ResultingEntity.Id!,
                ResultingEntity = updated.ResultingEntity
            };
        }
        catch (Exception exception)
        {
            return FailedCreate<T>(entity, exception);
        }
    }

    private async Task<OneOf<T, HttpResponseMessage, Exception>> GetReservedStandardEntityAsync<T>(
        T entity,
        Guid id,
        CancellationToken cancellationToken)
        where T : BusinessCentralDocument
    {
        var parent = typeof(T).GetCustomAttribute<BusinessCentralParentUrlAttribute>(inherit: true);
        if (parent is null)
            return await GetEntityAsync<T>(id, cancellationToken);

        var property = typeof(T).GetProperty(parent.ParentIdPropertyName, BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(entity) is Guid parentId && parentId != Guid.Empty
            ? await GetEntityAsync<T>(parentId, id, cancellationToken)
            : new InvalidOperationException(
                $"Reserved {typeof(T).Name} requires parent property '{parent.ParentIdPropertyName}'.");
    }

    private async Task<Guid?> TryFindReservationAsync(
        HttpClient httpClient,
        string reservationUrl,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var filter = EncodeQueryOptionValue($"correlationId eq {correlationId}");
        using var response = await httpClient.GetAsync($"{reservationUrl}?$filter={filter}&$top=2", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("value", out var value) ||
            value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 1)
            return null;
        return TryReadEntityId(value[0]);
    }

    private static Guid? TryReadEntityId(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return null;
        using var document = JsonDocument.Parse(responseBody);
        return TryReadEntityId(document.RootElement);
    }

    private static Guid? TryReadEntityId(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty("id", out var id) &&
        Guid.TryParse(id.GetString(), out var parsed)
            ? parsed
            : null;

    private string GetCorrelationReservationCollectionUrl(string entitySet)
    {
        var route = string.IsNullOrWhiteSpace(_config.Value.CorrelationApiRoute)
            ? BusinessCentralServiceOptions.DefaultCorrelationApiRoute
            : _config.Value.CorrelationApiRoute;
        return $"{route.Trim('/')}/companies({_config.Value.CompanyId})/{entitySet.Trim('/')}";
    }

    public async Task<OneOf<bool, HttpResponseMessage, Exception>> DeleteEntityAsync<T>(string entityId, CancellationToken cancellationToken = default) where T : BusinessCentralDocument
    {
        var url = $"{GetCollectionUrl<T>()}({entityId})";
        return await DeleteEntityAtUrlAsync(url, "*", cancellationToken);
    }

    public async Task<OneOf<bool, HttpResponseMessage, Exception>> DeleteEntityAsync<T>(
        T entity,
        CancellationToken cancellationToken = default)
        where T : BusinessCentralDocument
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            return new InvalidOperationException(
                $"Cannot delete Business Central {typeof(T).Name} because it has no record id.");
        }

        var url = $"{GetMutationCollectionUrl(entity)}({entity.Id})";
        return await DeleteEntityAtUrlAsync(url, entity.ETag ?? "*", cancellationToken);
    }

    private async Task<OneOf<bool, HttpResponseMessage, Exception>> DeleteEntityAtUrlAsync(
        string url,
        string etag,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("BusinessCentral");

        var response = await SendWithRetryAsync(httpClient, () =>
        {
            var retryRequest = new HttpRequestMessage(HttpMethod.Delete, url);
            retryRequest.Headers.TryAddWithoutValidation("If-Match", etag);
            return retryRequest;
        }, cancellationToken);

        if (!response.IsSuccessStatusCode) return response;
        response.Dispose();
        return true;
    }

    public async Task<OneOf<T, HttpResponseMessage, Exception>> GetEntityAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BusinessCentralDocument
        => await GetEntityAtParentAsync<T>(null, id, cancellationToken);

    public async Task<OneOf<T, HttpResponseMessage, Exception>> GetEntityAsync<T>(Guid parentId, Guid id, CancellationToken cancellationToken = default) where T : BusinessCentralDocument
        => await GetEntityAtParentAsync<T>(parentId, id, cancellationToken);

    private async Task<OneOf<T, HttpResponseMessage, Exception>> GetEntityAtParentAsync<T>(Guid? parentId, Guid id, CancellationToken cancellationToken) where T : BusinessCentralDocument
    {
        try
        {
            var response = parentId.HasValue
                ? await GetEntitiesAsync<T>(parentId.Value, $"id eq {id}", top: 1, cancellationToken: cancellationToken)
                : await GetEntitiesAsync<T>($"id eq {id}", top: 1, cancellationToken: cancellationToken);
            if (response.IsT2) return response.AsT2;
            if (response.IsT1) return response.AsT1;
            var entity = response.AsT0.Value.FirstOrDefault();
            if (entity is not null) return entity;
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                ReasonPhrase = $"Business Central {typeof(T).Name} {id} was not found."
            };
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public async Task<OneOf<ApiCollectionResponse<T>, HttpResponseMessage, Exception>> GetEntitiesAsync<T>(string? filter = null, int? skip = null, int? top = null, string? order = null, string? select = null, CancellationToken cancellationToken = default) where T : BusinessCentralDocument
        => await GetEntitiesAtUrlAsync<T>(GetCollectionUrl<T>(), filter, skip, top, order, select, cancellationToken);

    public async Task<OneOf<ApiCollectionResponse<T>, HttpResponseMessage, Exception>> GetEntitiesAsync<T>(Guid parentId, string? filter = null, int? skip = null, int? top = null, string? order = null, string? select = null, CancellationToken cancellationToken = default) where T : BusinessCentralDocument
        => await GetEntitiesAtUrlAsync<T>(GetParentCollectionUrl<T>(parentId), filter, skip, top, order, select, cancellationToken);

    private async Task<OneOf<ApiCollectionResponse<T>, HttpResponseMessage, Exception>> GetEntitiesAtUrlAsync<T>(string url, string? filter, int? skip, int? top, string? order, string? select, CancellationToken cancellationToken) where T : BusinessCentralDocument
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("BusinessCentral");

            var options = new List<string>();
            if (filter != null) options.Add($"$filter={EncodeQueryOptionValue(filter)}");
            if (skip != null) options.Add($"$skip={skip}");
            if (top != null) options.Add($"$top={top}");
            if (order != null) options.Add($"$orderby={EncodeQueryOptionValue(order)}");
            if (select != null) options.Add($"$select={EncodeQueryOptionValue(select)}");
            options.Add($"$count=true");

            url += $"?{string.Join("&", options)}";

            var values = new List<T>();
            int? count = null;
            string? nextUrl = url;
            var visitedLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (!string.IsNullOrWhiteSpace(nextUrl))
            {
                if (!visitedLinks.Add(nextUrl))
                {
                    return new InvalidDataException("Business Central returned a repeated @odata.nextLink.");
                }

                var currentUrl = nextUrl;
                var response = await SendWithRetryAsync(httpClient,
                    () => new HttpRequestMessage(HttpMethod.Get, currentUrl), cancellationToken);
                if (!response.IsSuccessStatusCode) return response;

                using (response)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var page = DeserializeCollection<T>(responseBody);

                    count ??= page.Count;
                    values.AddRange(page.Value);
                    nextUrl = page.NextLink;
                }

                if (top.HasValue && values.Count >= top.Value)
                {
                    values = values.Take(top.Value).ToList();
                    nextUrl = null;
                }
            }

            return new ApiCollectionResponse<T> { Count = count, Value = values };
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static string EncodeQueryOptionValue(string value) => Uri.EscapeDataString(value);

    public async Task<UpdateResult<T>> UpdateEntityAsync<T>(T entity, CancellationToken cancellationToken) where T : BusinessCentralDocument
    {
        var updateCreateEntitiesResponse = await UpdateEntitiesAsync(new List<T>() { entity }, cancellationToken);
        return updateCreateEntitiesResponse.First();
    }

    public async Task<List<UpdateResult<T>>> UpdateEntitiesAsync<T>(List<T> entities, CancellationToken cancellationToken) where T : BusinessCentralDocument
    {

        var httpClient = _httpClientFactory.CreateClient("BusinessCentral");

        var ret = new List<UpdateResult<T>>();

        foreach (var entity in entities)
        {
            try
            {
                var url = GetMutationCollectionUrl(entity);
                var requestUrl = $"{url}({entity.Id})";

                // The entity id identifies the OData resource in the request URL and the ETag is
                // carried by If-Match. Both are read-only response metadata and must not be sent in
                // the PATCH body (the standard Business Central APIs reject them).
                var patchProps = entity.GetAttributes().Select(attribute => attribute.Key).ToHashSet();

                var serializer = new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString,
                    Converters =
                    {
                        new DynamicallyIncludePropWhenPatchingConverter<T>(patchProps),
                        new EmptyStringJsonConverter()
                    }
                };

                var response = await SendWithRetryAsync(httpClient, () =>
                {
                    var retryMessage = new HttpRequestMessage(HttpMethod.Patch, requestUrl)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(entity, serializer),
                            System.Text.Encoding.UTF8,
                            "application/json")
                    };
                    retryMessage.Headers.TryAddWithoutValidation("if-match", entity.ETag ?? "*");
                    return retryMessage;
                }, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var bodyContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    var ex = new BusinessCentralHttpException(
                        response.StatusCode, $"update {typeof(T).Name}/{entity.Id}", bodyContent);

                    ret.Add(new UpdateResult<T>()
                    {
                        EntityId = entity.Id,
                        Exception = ex,
                        Success = false,
                        StatusCode = response.StatusCode
                    });
                    response.Dispose();
                    continue;
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                response.Dispose();
                var result = JsonSerializer.Deserialize<T>(responseBody, _defaultSerializerOptions);
                if (result is null || string.IsNullOrWhiteSpace(result.Id))
                {
                    ret.Add(new UpdateResult<T>
                    {
                        EntityId = entity.Id,
                        Exception = new InvalidDataException(
                            "Business Central returned an incomplete update response without a record id."),
                        Success = false
                    });
                    continue;
                }
                ret.Add(new UpdateResult<T>()
                {
                    EntityId = entity.Id,
                    ResultingEntity = result,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                ret.Add(new UpdateResult<T>()
                {
                    EntityId = entity.Id,
                    Exception = ex,
                    Success = false
                });
            }
            finally
            {
                entity.ETag = null;
            }
        }

        return ret;
    }

    private string GetCollectionUrl<T>() where T : BusinessCentralDocument
    {
        var route = GetApiRoute<T>();
        var entitySet = typeof(T).GetCustomAttribute<BusinessCentralUrlAttribute>(inherit: true)?.Url;

        if (string.IsNullOrWhiteSpace(entitySet))
        {
            entitySet = typeof(T).Name;
        }

        return $"{route.Trim('/')}/companies({_config.Value.CompanyId})/{entitySet.Trim('/')}";
    }

    private string GetMutationCollectionUrl<T>(T entity) where T : BusinessCentralDocument
    {
        var parentUrl = typeof(T).GetCustomAttribute<BusinessCentralParentUrlAttribute>(inherit: true);
        if (parentUrl is null)
        {
            return GetCollectionUrl<T>();
        }

        var parentIdProperty = typeof(T).GetProperty(
            parentUrl.ParentIdPropertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (parentIdProperty is null)
        {
            throw new InvalidOperationException(
                $"Business Central parent route for {typeof(T).Name} refers to missing property " +
                $"'{parentUrl.ParentIdPropertyName}'.");
        }

        var parentId = parentIdProperty.GetValue(entity)?.ToString();
        if (string.IsNullOrWhiteSpace(parentId) ||
            string.Equals(parentId, Guid.Empty.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Business Central {typeof(T).Name} requires '{parentUrl.ParentIdPropertyName}' " +
                "for a parent-scoped write.");
        }

        var route = GetApiRoute<T>();
        var entitySet = typeof(T).GetCustomAttribute<BusinessCentralUrlAttribute>(inherit: true)?.Url;
        if (string.IsNullOrWhiteSpace(entitySet))
        {
            entitySet = typeof(T).Name;
        }

        return $"{route.Trim('/')}/companies({_config.Value.CompanyId})/" +
            $"{parentUrl.ParentUrl.Trim('/')}({parentId})/{entitySet.Trim('/')}";
    }

    private string GetParentCollectionUrl<T>(Guid parentId) where T : BusinessCentralDocument
    {
        if (parentId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Business Central parent-scoped read for {typeof(T).Name} requires a parent id.");
        }

        var parentUrl = typeof(T).GetCustomAttribute<BusinessCentralParentUrlAttribute>(inherit: true)
            ?? throw new InvalidOperationException(
                $"Business Central {typeof(T).Name} does not define a parent route.");
        var route = GetApiRoute<T>();
        var entitySet = typeof(T).GetCustomAttribute<BusinessCentralUrlAttribute>(inherit: true)?.Url;
        if (string.IsNullOrWhiteSpace(entitySet))
        {
            entitySet = typeof(T).Name;
        }

        return $"{route.Trim('/')}/companies({_config.Value.CompanyId})/" +
            $"{parentUrl.ParentUrl.Trim('/')}({parentId})/{entitySet.Trim('/')}";
    }

    private string GetApiRoute<T>() where T : BusinessCentralDocument
    {
        var entityRoute = typeof(T)
            .GetCustomAttribute<BusinessCentralApiRouteAttribute>(inherit: true)?
            .ApiRoute;
        if (!string.IsNullOrWhiteSpace(entityRoute))
        {
            return entityRoute;
        }

        return string.IsNullOrWhiteSpace(_config.Value.ApiRoute)
            ? BusinessCentralServiceOptions.DefaultApiRoute
            : _config.Value.ApiRoute;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        var maximumAttempts = Math.Max(1, _config.Value.MaxRetryAttempts);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var request = requestFactory();
                var response = await client.SendAsync(request, cancellationToken);
                if (!IsTransient(response.StatusCode) || attempt >= maximumAttempts)
                {
                    return response;
                }

                var delay = GetRetryDelay(response, attempt);
                response.Dispose();
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex) when (IsTransient(ex, cancellationToken) && attempt < maximumAttempts)
            {
                await Task.Delay(GetRetryDelay(null, attempt), cancellationToken);
            }
        }
    }

    private async Task<T?> TryFindCreatedEntityAsync<T>(CreateRecoveryKey recoveryKey, CancellationToken cancellationToken)
        where T : BusinessCentralDocument
    {
        var filter = string.Join(" and ", recoveryKey.Parts.Select(part =>
            $"{part.FieldName} eq {FormatODataLiteral(part.Value)}"));
        var response = await GetEntitiesAsync<T>(
            filter,
            top: 2,
            cancellationToken: cancellationToken);
        if (!response.IsT0 || response.AsT0.Value.Count != 1) return null;

        var recovered = response.AsT0.Value[0];
        return !string.IsNullOrWhiteSpace(recovered.Id) && RecoveryKeyMatches(recovered, recoveryKey)
            ? recovered
            : null;
    }

    private static CreateRecoveryKey? GetCreateRecoveryKey(BusinessCentralDocument entity)
    {
        var attributedProperties = entity.GetType().GetProperties()
            .Select(property => new
            {
                Property = property,
                Attribute = property.GetCustomAttribute<BusinessCentralCreateRecoveryKeyAttribute>(inherit: true)
            })
            .Where(candidate => candidate.Attribute is not null)
            .ToList();
        if (attributedProperties.Count > 0)
        {
            var parts = new List<CreateRecoveryKeyPart>(attributedProperties.Count);
            foreach (var attributedProperty in attributedProperties)
            {
                var value = attributedProperty.Property.GetValue(entity);
                if (value is null || value is string text && string.IsNullOrWhiteSpace(text))
                {
                    // A partial composite key is not safe: it may identify another record.
                    return null;
                }

                parts.Add(new CreateRecoveryKeyPart(
                    attributedProperty.Attribute!.FieldName,
                    value));
            }

            return new CreateRecoveryKey(parts);
        }

        var number = entity.GetAttributes()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, "number", StringComparison.OrdinalIgnoreCase))
            .Value?.ToString();
        return string.IsNullOrWhiteSpace(number)
            ? null
            : new CreateRecoveryKey([new CreateRecoveryKeyPart("number", number)]);
    }

    private static bool RecoveryKeyMatches(
        BusinessCentralDocument entity,
        CreateRecoveryKey recoveryKey)
    {
        var attributes = entity.GetAttributes();
        foreach (var part in recoveryKey.Parts)
        {
            var actual = attributes.FirstOrDefault(attribute =>
                string.Equals(attribute.Key, part.FieldName, StringComparison.OrdinalIgnoreCase)).Value;
            if (actual is null || !RecoveryValuesEqual(actual, part.Value)) return false;
        }

        return true;
    }

    private static bool RecoveryValuesEqual(object actual, object expected)
    {
        if (actual is Guid actualGuid && expected is Guid expectedGuid)
        {
            return actualGuid == expectedGuid;
        }

        return string.Equals(
            Convert.ToString(actual, System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    private static string FormatODataLiteral(object value) => value switch
    {
        string text => $"'{text.Replace("'", "''", StringComparison.Ordinal)}'",
        char character => $"'{character.ToString().Replace("'", "''", StringComparison.Ordinal)}'",
        Guid guid => guid.ToString(),
        bool boolean => boolean ? "true" : "false",
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => $"'{value.ToString()!.Replace("'", "''", StringComparison.Ordinal)}'"
    };

    private sealed record CreateRecoveryKey(IReadOnlyList<CreateRecoveryKeyPart> Parts);

    private sealed record CreateRecoveryKeyPart(string FieldName, object Value);

    private ApiCollectionResponse<T> DeserializeCollection<T>(string responseBody)
    {
        using var json = JsonDocument.Parse(responseBody);
        if (json.RootElement.ValueKind != JsonValueKind.Object ||
            !json.RootElement.TryGetProperty("value", out var value) ||
            value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                "Business Central returned a malformed collection response without a value array.");
        }

        return JsonSerializer.Deserialize<ApiCollectionResponse<T>>(responseBody, _defaultSerializerOptions)
            ?? throw new InvalidDataException("Business Central returned an empty collection response.");
    }

    private static CreateResult<T> FailedCreate<T>(T entity, Exception exception)
        where T : BusinessCentralDocument => new()
        {
            EntityId = entity.Id ?? string.Empty,
            Exception = exception,
            Success = false
        };

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;

    private static bool IsAmbiguousCreateResponse(HttpStatusCode statusCode) =>
        (int)statusCode >= 500;

    private static bool IndicatesEntityAlreadyExists(HttpStatusCode statusCode, string responseBody) =>
        statusCode == HttpStatusCode.Conflict ||
        statusCode == HttpStatusCode.BadRequest &&
        (responseBody.Contains("Internal_EntityWithSameKeyExists", StringComparison.OrdinalIgnoreCase) ||
         responseBody.Contains("already exists. Identification fields and values", StringComparison.OrdinalIgnoreCase));

    private static bool IsTransient(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException ||
        (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    private TimeSpan GetRetryDelay(HttpResponseMessage? response, int attempt)
    {
        var retryAfter = response?.Headers.RetryAfter?.Delta;
        if (retryAfter.HasValue && retryAfter.Value >= TimeSpan.Zero) return retryAfter.Value;
        var milliseconds = Math.Max(0, _config.Value.RetryBaseDelayMilliseconds);
        return TimeSpan.FromMilliseconds(milliseconds * Math.Pow(2, Math.Max(0, attempt - 1)));
    }
}
