using System.Text.Json;
using System.Text.Json.Serialization;
using EntityResolutionSource.Senzing.Models.Typedef;

namespace EntityResolutionSource.Senzing.Models;

// The V3 recommended flatten JSON structure to feed Senzing, which sucks.
// By using suffixing for composite types e.g. ADDR_STATE_HOME, ADDR_STATE_BUSINESS.
// The generated types don't support this. V4 has moderately improved this. See NestedRecord.

public class RecordV3(string dataSourceCode, string recordId, SenzingEntitySpecification entity)
    : AbstractRecord(recordId, dataSourceCode)
{
    public SenzingEntitySpecification Entity { get; private set; } = entity;

    [JsonIgnore]
    public string Json => JsonSerializer.Serialize(Entity);
}
