using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Senzing.Sdk;
using Senzing.Sdk.Core;
using Senzing.Typedef;
using SzConnyApp.SenzingV4.Models;
using static Senzing.Sdk.SzFlags;

namespace SzConnyApp.SenzingV4.Senzing;

public sealed class SzEnvironmentWrapper : ISzEnvironmentWrapper
{
    private readonly SzEnvironment _szEnvironment;
    private readonly ILogger _logger;
    private bool _isDisposed;

    public SzEnvironmentWrapper(ILogger<SzEnvironmentWrapper> logger)
    {
        _logger = logger;
        _szEnvironment = SetupEnvironment();
        SetupDefaultConfiguration();
    }

    private SzEnvironment SetupEnvironment()
    {
        var settings = Environment.GetEnvironmentVariable("SENZING_ENGINE_CONFIGURATION_JSON");
        var assembly = Assembly.GetExecutingAssembly();
        var instanceName = assembly.GetName().Name;
        return SzCoreEnvironment.NewBuilder().Settings(settings).InstanceName(instanceName).Build();
    }

    private void SetupDefaultConfiguration()
    {
        var configMgr = _szEnvironment.GetConfigManager();
        var defaultConfigId = configMgr.GetDefaultConfigID();
        if (defaultConfigId > 0)
        {
            _logger.LogInformation($"Senzing default config already exists: {defaultConfigId}");
            return;
        }

        var config = configMgr.CreateConfig();
        var configDefinition = config.Export();
        configMgr.SetDefaultConfig(configDefinition);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            try
            {
                _szEnvironment.Destroy();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during SzEngine destruction: {ex.Message}");
            }
        }

        _isDisposed = true;
    }

    public SzEngine Engine => _szEnvironment.GetEngine();

    public SzDiagnostic Diagnostic => _szEnvironment.GetDiagnostic();

    public SzEngineAddRecordResponse? AddFlatRecord(FlatRecord flatRecord)
    {
        return AddRecord(flatRecord.DataSourceCode, flatRecord.RecordId, flatRecord.Json);
    }

    public SzEngineAddRecordResponse? AddNestedRecord(NestedRecord nestedRecord)
    {
        return AddRecord(nestedRecord.DataSourceCode, nestedRecord.RecordId, nestedRecord.Json);
    }

    private SzEngineAddRecordResponse? AddRecord(
        string dataSourceCode,
        string recordId,
        string recordJsonString
    )
    {
        var addRecordResponseJsonString = Engine.AddRecord(
            dataSourceCode,
            recordId,
            recordJsonString,
            SzFlag.SzWithInfo
        );
        return JsonSerializer.Deserialize<SzEngineAddRecordResponse>(addRecordResponseJsonString);
    }

    public NestedRecord GetRecord(string dataSourceCode, string recordId)
    {
        var getRecordResponseJsonString = Engine.GetRecord(
            dataSourceCode,
            recordId,
            SzRecordDefaultFlags
        );
        
        var recordResponse = JsonSerializer.Deserialize<RecordResponse>(getRecordResponseJsonString);
        
        return NestedRecord.FromRecordResponse(recordResponse);
    }
}
