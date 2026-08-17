using System.Runtime.CompilerServices;
using Xunit;

namespace Reimaginate.DataHub.Agent.TestFramework.BusinessCentral.IntegrationTesting.Xunit;

public sealed class BusinessCentralFactAttribute : FactAttribute
{
    public BusinessCentralFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        SkipExceptions = [typeof(BusinessCentralIntegrationTestSkippedException)];
    }
}
