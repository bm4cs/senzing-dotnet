using System.Text.Json;
using System.Text.Json.Serialization;
using EntityResolutionSource.Senzing.Models.Typedef;

namespace EntityResolutionSource.Senzing.Models;

/// <summary>
/// Typed Senzing V4 Record representation.
/// Recommended JSON Schema as per https://senzing.com/docs/entity_specification/
/// In prior versions (V3 and prior) we allowed a flat JSON structure with a separate sub-list for each feature that had multiple values.
/// While we still support that, we now (V4+) recommend the following JSON schema that has just one list for all features.
/// </summary>
/// <param name="dataSourceCode"></param>
/// <param name="recordId"></param>
/// <param name="features"></param>
public class RecordV4 : AbstractRecord
{
    public RecordV4()
    {
        Features = new List<SenzingEntitySpecification>();
    }

    public RecordV4(
        string dataSourceCode,
        string recordId,
        IList<SenzingEntitySpecification> features
    )
        : base(recordId, dataSourceCode)
    {
        Features = features;
    }

    [JsonPropertyName("FEATURES")]
    public IList<SenzingEntitySpecification> Features { get; set; }

    [JsonIgnore]
    public string Json =>
        JsonSerializer.Serialize(
            this,
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            }
        );

    public static RecordV4 FromRecordResponse(GetRecordResponse getRecordResponse)
    {
        return new RecordV4(
            getRecordResponse.DataSourceCode,
            getRecordResponse.RecordId,
            getRecordResponse.Features
        );
    }
}
