using System.Text.Json.Serialization;

namespace SzConnyApp.SenzingV4.Models;

public abstract class AbstractRecord(string recordId, string dataSourceCode)
{
    [JsonPropertyName("RECORD_ID")]
    public string RecordId { get; private set; } = recordId;

    [JsonPropertyName("DATA_SOURCE")]
    public string DataSourceCode { get; private set; } = dataSourceCode;
}
