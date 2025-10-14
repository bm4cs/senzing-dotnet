using System.Reflection;
using Microsoft.Extensions.Logging;
using Senzing.Sdk;
using Senzing.Sdk.Core;

namespace SzConnyApp.SenzingV4.Senzing;

public class SzEnvironmentWrapper : ISzEnvironmentWrapper
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

    protected virtual void Dispose(bool disposing)
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

    public SzEngine Engine
    {
        get { return _szEnvironment.GetEngine(); }
    }
}
