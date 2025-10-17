using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senzing.Sdk;
using Senzing.Sdk.Core;
using SzConnyApp.SenzingV4;
using SzConnyApp.SenzingV4.Commands;
using SzConnyApp.SenzingV4.Senzing;

namespace SzConnyApp;

public static class DependencyInjection
{
    public static void ConfigureSenzing(this IServiceCollection services)
    {
        services.AddTransient<ISzEnvironmentWrapper, SzEnvironmentWrapper>();
        services.AddTransient<IGetEntityCommand, GetEntityCommand>();
        services.AddTransient<IRecordLoaderCommand, RecordLoaderCommand>();
        services.AddTransient<IRepositoryPurgerCommand, RepositoryPurgerCommand>();
        services.AddTransient<ISearchCommand, SearchCommand>();
    }

    public static void ConfigureLogging(this IServiceCollection services)
    {
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder
                .AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                })
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning);
        });
    }
}
