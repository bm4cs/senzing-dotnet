using Senzing.Sdk;

namespace SzConnyApp.SenzingV4.Senzing;

public interface ISzEnvironmentWrapper : IDisposable
{
    SzEngine Engine { get; }

    SzDiagnostic Diagnostic { get; }
}
