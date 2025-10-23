namespace SzConnyApp.SenzingV4.Models;

public sealed class DumbRecord(string dataSourceCode, string recordId, string jsonText)
    : AbstractRecord(recordId, dataSourceCode)
{
    public string JsonText { get; private set; } = jsonText;
}
