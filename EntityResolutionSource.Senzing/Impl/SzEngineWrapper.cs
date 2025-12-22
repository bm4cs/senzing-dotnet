using System.Reflection;
using System.Text.Json;
using EntityResolutionSource.Senzing.Core;
using EntityResolutionSource.Senzing.Interfaces;
using EntityResolutionSource.Senzing.Models;
using EntityResolutionSource.Senzing.Models.Typedef;
using Microsoft.Extensions.Logging;
using Senzing.Sdk;
using Senzing.Sdk.Core;
using static Senzing.Sdk.SzFlags;

namespace EntityResolutionSource.Senzing.Impl;

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
        config.RegisterDataSource(SzConstants.DataSources.Ems);
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

    public async Task<SzEngineAddRecordResponse?> AddRecordV4Async(RecordV4 recordV4)
    {
        return await Task.Run(() =>
        {
            var addRecordResponseJsonString = Engine.AddRecord(
                recordV4.DataSourceCode,
                recordV4.RecordId,
                recordV4.Json,
                SzFlag.SzWithInfo
            );
            return JsonSerializer.Deserialize<SzEngineAddRecordResponse>(
                addRecordResponseJsonString
            );
        });
    }

    public async Task<RecordV4?> GetRecordV4Async(string dataSourceCode, string recordId)
    {
        return await Task.Run(() =>
        {
            var getRecordResponseJsonString = Engine.GetRecord(
                dataSourceCode,
                recordId,
                SzRecordDefaultFlags
            );

            var recordResponse = JsonSerializer.Deserialize<GetRecordResponse>(
                getRecordResponseJsonString
            );

            return recordResponse != null
                ? RecordV4.FromRecordResponse(recordResponse)
                : new RecordV4();
        });
    }

    public async Task<SzEngineGetEntityByEntityIdResponse?> GetEntityV4Async(long entityId)
    {
        return await Task.Run(() =>
        {
            // SzEntityDefaultFlags =
            //   SzEntityIncludePossiblySameRelations |
            //   SzEntityIncludePossiblyRelatedRelations |
            //   SzEntityIncludeNameOnlyRelations |
            //   SzEntityIncludeDisclosedRelations |
            //   SzEntityIncludeRepresentativeFeatures |
            //   SzEntityIncludeEntityName |
            //   SzEntityIncludeRecordSummary |
            //   SzEntityIncludeRecordData |
            //   SzEntityIncludeRecordMatchingInfo |
            //   SzEntityIncludeRelatedEntityName |
            //   SzEntityIncludeRelatedMatchingInfo |
            //   SzEntityIncludeRelatedRecordSummary

            var getEntityResponseJsonString = Engine.GetEntity(entityId, SzEntityDefaultFlags);

            return JsonSerializer.Deserialize<SzEngineGetEntityByEntityIdResponse>(
                getEntityResponseJsonString
            );
        });
    }
}
