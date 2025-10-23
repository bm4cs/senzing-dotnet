using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senzing.Typedef;
using SzConnyApp;
using SzConnyApp.Ontology;
using SzConnyApp.SenzingV4;
using SzConnyApp.SenzingV4.Commands;
using SzConnyApp.SenzingV4.Models;
using SzConnyApp.SenzingV4.Senzing;

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
            .Default.ParseArguments<
                GetEntityOptions,
                ExportRecordsOptions,
                ForceOptions,
                LoadOptions,
                PurgeOptions,
                SearchOptions
            >(args)
            .MapResult(
                (ExportRecordsOptions opts) =>
                    RunCommand(serviceProvider.GetService<IExportRecordsCommand>()),
                (LoadOptions opts) =>
                    RunCommand(serviceProvider.GetService<IRecordLoaderCommand>()),
                (PurgeOptions opts) =>
                    RunCommand(serviceProvider.GetService<IRepositoryPurgerCommand>()),
                (SearchOptions opts) => RunCommand(serviceProvider.GetService<ISearchCommand>()),
                (ForceOptions opts) =>
                    RunCommand(serviceProvider.GetService<IForceResolveCommand>()),
                (GetEntityOptions opts) =>
                {
                    var command = serviceProvider.GetService<IGetEntityCommand>();
                    command.EntityId = opts.EntityId;
                    return RunCommand(command);
                },
                _ => 1
            );
    }

    private static int RunCommand(ICommand? command)
    {
        try
        {
            command?.Execute();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error executing command");
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

[Verb("getentity", HelpText = "Retrieve an entity by ID.")]
class GetEntityOptions : IOptions
{
    [Option('i', "id", Required = true, HelpText = "ID of entity to retrieve.")]
    public long EntityId { get; set; }
}

[Verb("export", HelpText = "Export all entities in the Senzing corpus.")]
class ExportRecordsOptions : IOptions { }

[Verb("force", HelpText = "Force resolve (merge) scenario.")]
class ForceOptions : IOptions { }
