using Senzing.Sdk;
using Senzing.Typedef;
using SzConnyApp.SenzingV4.Models;

namespace SzConnyApp.SenzingV4.Senzing;

public interface ISzEnvironmentWrapper : IDisposable
{
    SzEngine Engine { get; }

    SzDiagnostic Diagnostic { get; }

    public SzEngineAddRecordResponse? AddFlatRecord(FlatRecord flatRecord);

    public SzEngineAddRecordResponse? AddNestedRecord(NestedRecord nestedRecord);

    public NestedRecord GetRecord(string dataSourceCode, string recordId);
}
