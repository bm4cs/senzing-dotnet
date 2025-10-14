using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SzConnyApp;
using SzConnyApp.Ontology;
using SzConnyApp.SenzingV4;
using SzConnyApp.SenzingV4.Commands;



public class EntryPoint
{
    static int Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.ConfigureSenzing();
        serviceCollection.ConfigureLogging();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = serviceProvider.GetService<ILogger<EntryPoint>>();

        logger?.LogInformation("ConnyApp v0.1");


        // switch (Parser.Default.ParseArguments<LoadOptions, PurgeOptions>(args).Value.GetType())
        // {
        //     PurgeOptions => { logger.LogWarning("TODO purge"); },
        //     _ => throw new ArgumentException("boom")
        // }
        
        return 0;
    }
}








// var ontologyInstancePaths = new[] { "Ontology/Instances/v1/ontology-v20251001.json" };
// foreach (var ontologyPath in ontologyInstancePaths)
// {
//     var isValid = Ontology.Validate(ontologyPath);
//     Console.WriteLine($"'{ontologyPath}' JSON schema check: {(isValid ? "PASS" : "FAIL")}");
// }

[Verb("purge", HelpText = "Purge the Senzing repository.")]
class PurgeOptions
{
}

[Verb("load", HelpText = "Load up the Senzing repository with sample records.")]
class LoadOptions
{
}




