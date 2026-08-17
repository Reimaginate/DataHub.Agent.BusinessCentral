using Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared.Mapping;
using Reimaginate.Mapper;

namespace Reimaginate.DataHub.Agent.BusinessCentral.Tests.Shared;

[Mapper]
[ScanAssembly(typeof(MapAccountToCustomer))]
public partial class Mapper;
