using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SzConnyApp;
using SzConnyApp.Ontology;
using SzConnyApp.SenzingV4;
using SzConnyApp.SenzingV4.Commands;

// private static ILogger logger;

public class EntryPoint
{
    private static ILogger _logger;

    static int Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.ConfigureSenzing();
        serviceCollection.ConfigureLogging();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        _logger = serviceProvider.GetService<ILogger<EntryPoint>>();
        _logger?.LogInformation("ConnyApp v0.1");

        return Parser
            .Default.ParseArguments<LoadOptions, PurgeOptions, SearchOptions>(args)
            .MapResult(
                (LoadOptions opts) =>
                    RunCommand(serviceProvider.GetService<IRecordLoaderCommand>()),
                (PurgeOptions opts) =>
                    RunCommand(serviceProvider.GetService<IRepositoryPurgerCommand>()),
                (SearchOptions opts) =>
                    RunCommand(serviceProvider.GetService<ISearchCommand>()),
                _ => 1
            );
    }

    private static int RunCommand(ICommand? command)
    {
        try
        {
            _logger.LogInformation($"Starting command {command?.GetType().Name}");
            command?.Execute();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error executing command");
        }
        finally
        {
            _logger.LogInformation($"Completed command {command?.GetType().Name}");
        }
        return 0;
    }
}

// var ontologyInstancePaths = new[] { "Ontology/Instances/v1/ontology-v20251001.json" };
// foreach (var ontologyPath in ontologyInstancePaths)
// {
//     var isValid = Ontology.Validate(ontologyPath);
//     Console.WriteLine($"'{ontologyPath}' JSON schema check: {(isValid ? "PASS" : "FAIL")}");
// }

interface IOptions { }

[Verb("purge", HelpText = "Purge the Senzing repository.")]
class PurgeOptions : IOptions { }

[Verb("load", HelpText = "Load up the Senzing repository with sample records.")]
class LoadOptions : IOptions { }

[Verb("search", HelpText = "Search for entities using the mapped JSON entity specification.")]
class SearchOptions : IOptions { }
