namespace SzConnyApp.SenzingV4.Models;

public sealed class DumbRecord(string dataSourceCode, string recordId, string jsonText)
{
    public string RecordId { get; private set; } = recordId;

    public string DataSourceCode { get; private set; } = dataSourceCode;

    public string JsonText { get; private set; } = jsonText;
}
