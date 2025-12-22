using EntityResolutionSource.Senzing.Models;
using EntityResolutionSource.Senzing.Models.Typedef;
using Senzing.Sdk;

namespace EntityResolutionSource.Senzing.Interfaces;

public interface ISzEnvironmentWrapper : IDisposable
{
    //TODO: language-ext functional refactor

    SzEngine Engine { get; }

    SzDiagnostic Diagnostic { get; }

    Task<SzEngineAddRecordResponse?> AddRecordV4Async(RecordV4 recordV4);

    public Task<RecordV4?> GetRecordV4Async(string dataSourceCode, string recordId);

    public Task<SzEngineGetEntityByEntityIdResponse?> GetEntityV4Async(long entityId);
}
