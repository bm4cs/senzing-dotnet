using System.Text.Json;
using System.Text.Json.Serialization;
using EntityResolutionSource.Senzing.Models.Typedef;

namespace EntityResolutionSource.Senzing.Models;

public class GetRecordResponse(string recordId, string dataSourceCode)
{
    [JsonPropertyName("RECORD_ID")]
    public string RecordId { get; private set; } = recordId;

    [JsonPropertyName("DATA_SOURCE")]
    public string DataSourceCode { get; private set; } = dataSourceCode;

    [JsonPropertyName("JSON_DATA")]
    public IDictionary<string, object> JsonData { get; set; }

    [JsonIgnore]
    public SenzingEntitySpecification[] Features
    {
        get
        {
            return JsonData
                    .Where(kvp =>
                        kvp.Value is JsonElement
                        && ((JsonElement)kvp.Value).ValueKind == JsonValueKind.Array
                    )
                    .Select(kvp =>
                        ((JsonElement)kvp.Value).Deserialize<SenzingEntitySpecification[]>()
                    )
                    .FirstOrDefault()
                ?? [];
        }
    }
}
