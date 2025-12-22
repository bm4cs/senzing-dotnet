using System.Text.Json.Serialization;

namespace EntityResolutionSource.Senzing.Models;

public abstract class AbstractRecord
{
    public AbstractRecord() { }

    public AbstractRecord(string recordId, string dataSourceCode)
    {
        RecordId = recordId;
        DataSourceCode = dataSourceCode;
    }

    [JsonPropertyName("RECORD_ID")]
    public string RecordId { get; set; }

    [JsonPropertyName("DATA_SOURCE")]
    public string DataSourceCode { get; set; }
}
