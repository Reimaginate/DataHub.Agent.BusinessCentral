using Reimaginate.DataHub.SharedModels.Attributes;
using Reimaginate.DataHub.SharedModels.Core;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Models.DataHub;

[RelatedEntityType("BusinessCentral", "PaymentTerm")]
public sealed class PaymentTerm : DataHubEntity
{
    public PaymentTerm() => entityType = nameof(PaymentTerm);
    public string? Code { get; set; }
    public string? DisplayName { get; set; }
    public string? DueDateCalculation { get; set; }
    public string? DiscountDateCalculation { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool? CalculateDiscountOnCreditMemos { get; set; }
}
