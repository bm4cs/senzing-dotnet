using System.Text.Json;
using System.Text.Json.Serialization;
using Senzing.Typedef;

namespace SzConnyApp.SenzingV4.Models;

public class NestedRecord(
    string dataSourceCode,
    string recordId,
    IList<SenzingEntitySpecification> features
) : AbstractRecord(recordId, dataSourceCode)
{
    [JsonPropertyName("FEATURES")]
    public IList<SenzingEntitySpecification> Features { get; set; } = features;

    [JsonIgnore]
    public string Json =>
        JsonSerializer.Serialize(
            this,
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            }
        );

    public static NestedRecord FromRecordResponse(RecordResponse recordResponse)
    {
        return new NestedRecord(recordResponse.DataSourceCode, recordResponse.RecordId, recordResponse.Features);
    }
}
