using System.Text.Json.Serialization;
using Reimaginate.DataHub.Agent.BusinessCentral.Abstractions.Models;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Reference.Models.BusinessCentral;

[BusinessCentralUrl("customers")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class Customer : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonPropertyName("number")]
    public string? Number
    {
        get => GetAttributeValue<string>("number");
        set => SetWithNotification("number", value);
    }

    [JsonPropertyName("displayName")]
    public string? DisplayName
    {
        get => GetAttributeValue<string>("displayName");
        set => SetWithNotification("displayName", value);
    }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber
    {
        get => GetAttributeValue<string>("phoneNumber");
        set => SetWithNotification("phoneNumber", value);
    }

    [JsonPropertyName("email")]
    public string? Email
    {
        get => GetAttributeValue<string>("email");
        set => SetWithNotification("email", value);
    }

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime
    {
        get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime");
        set => SetWithNotification("lastModifiedDateTime", value);
    }

    [JsonIgnore]
    public DateTimeOffset? LastModifiedAt
    {
        get => LastModifiedDateTime;
        set => LastModifiedDateTime = value;
    }
}

[BusinessCentralUrl("items")]
[BusinessCentralLastModified("lastModifiedDateTime")]
public sealed class Item : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonPropertyName("number")]
    public string? Number
    {
        get => GetAttributeValue<string>("number");
        set => SetWithNotification("number", value);
    }

    [JsonPropertyName("displayName")]
    public string? DisplayName
    {
        get => GetAttributeValue<string>("displayName");
        set => SetWithNotification("displayName", value);
    }

    [JsonPropertyName("type")]
    public string? Type
    {
        get => GetAttributeValue<string>("type");
        set => SetWithNotification("type", value);
    }

    [JsonPropertyName("unitPrice")]
    public decimal? UnitPrice
    {
        get => GetAttributeValue<decimal?>("unitPrice");
        set => SetWithNotification("unitPrice", value);
    }

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime
    {
        get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime");
        set => SetWithNotification("lastModifiedDateTime", value);
    }

    [JsonIgnore]
    public DateTimeOffset? LastModifiedAt
    {
        get => LastModifiedDateTime;
        set => LastModifiedDateTime = value;
    }
}

[BusinessCentralUrl("salesOrders")]
[BusinessCentralLastModified("lastModifiedDateTime")]
[BusinessCentralCreateReservation("salesDocumentReservations", "Order")]
public sealed class SalesOrder : BusinessCentralDocument, IBusinessCentralIncrementalEntity
{
    [JsonIgnore]
    [BusinessCentralReservationField("correlationId")]
    public Guid? DataHubCorrelationId { get; set; }

    [JsonPropertyName("number")]
    public string? Number
    {
        get => GetAttributeValue<string>("number");
        set => SetWithNotification("number", value);
    }

    [JsonPropertyName("externalDocumentNumber")]
    public string? ExternalDocumentNumber
    {
        get => GetAttributeValue<string>("externalDocumentNumber");
        set => SetWithNotification("externalDocumentNumber", value);
    }

    [JsonPropertyName("orderDate")]
    [BusinessCentralDate]
    public string? OrderDate
    {
        get => GetAttributeValue<string>("orderDate");
        set => SetWithNotification("orderDate", value);
    }

    [JsonPropertyName("customerId")]
    [BusinessCentralReservationField("customerId")]
    public Guid? CustomerId
    {
        get => GetAttributeValue<Guid?>("customerId");
        set => SetWithNotification("customerId", value);
    }

    [JsonPropertyName("status")]
    public string? Status
    {
        get => GetAttributeValue<string>("status");
        set => SetWithNotification("status", value);
    }

    [JsonPropertyName("totalAmountIncludingTax")]
    public decimal? TotalAmountIncludingTax
    {
        get => GetAttributeValue<decimal?>("totalAmountIncludingTax");
        set => SetWithNotification("totalAmountIncludingTax", value);
    }

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime
    {
        get => GetAttributeValue<DateTimeOffset?>("lastModifiedDateTime");
        set => SetWithNotification("lastModifiedDateTime", value);
    }

    [JsonIgnore]
    public DateTimeOffset? LastModifiedAt
    {
        get => LastModifiedDateTime;
        set => LastModifiedDateTime = value;
    }
}

[BusinessCentralUrl("salesOrderLines")]
[BusinessCentralCreateReservation("salesDocumentLineReservations", "Order")]
public sealed class SalesOrderLine : BusinessCentralDocument
{
    [JsonIgnore]
    [BusinessCentralReservationField("correlationId")]
    public Guid? DataHubCorrelationId { get; set; }

    [JsonPropertyName("documentId")]
    [BusinessCentralReservationField("documentId")]
    public Guid? DocumentId
    {
        get => GetAttributeValue<Guid?>("documentId");
        set => SetWithNotification("documentId", value);
    }

    [JsonPropertyName("itemId")]
    [BusinessCentralReservationField("itemId")]
    public Guid? ItemId
    {
        get => GetAttributeValue<Guid?>("itemId");
        set => SetWithNotification("itemId", value);
    }

    [JsonPropertyName("lineType")]
    public string? LineType
    {
        get => GetAttributeValue<string>("lineType");
        set => SetWithNotification("lineType", value);
    }

    [JsonPropertyName("sequence")]
    public int? Sequence
    {
        get => GetAttributeValue<int?>("sequence");
        set => SetWithNotification("sequence", value);
    }

    [JsonPropertyName("description")]
    public string? Description
    {
        get => GetAttributeValue<string>("description");
        set => SetWithNotification("description", value);
    }

    [JsonPropertyName("quantity")]
    public decimal? Quantity
    {
        get => GetAttributeValue<decimal?>("quantity");
        set => SetWithNotification("quantity", value);
    }

    [JsonPropertyName("unitPrice")]
    public decimal? UnitPrice
    {
        get => GetAttributeValue<decimal?>("unitPrice");
        set => SetWithNotification("unitPrice", value);
    }

    [JsonPropertyName("amountIncludingTax")]
    public decimal? AmountIncludingTax
    {
        get => GetAttributeValue<decimal?>("amountIncludingTax");
        set => SetWithNotification("amountIncludingTax", value);
    }
}
