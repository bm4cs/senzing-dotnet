using Microsoft.Extensions.DependencyInjection;
using SzConnyApp;
using SzConnyApp.Ontology;
using SzConnyApp.SenzingV4;

Console.WriteLine("ConnyApp v0.1");

var serviceCollection = new ServiceCollection();
serviceCollection.ConfigureSenzing();
serviceCollection.ConfigureLogging();
var serviceProvider = serviceCollection.BuildServiceProvider();

var recordLoader = serviceProvider.GetService<IRecordLoader>();
recordLoader?.Execute();

// var ontologyInstancePaths = new[] { "Ontology/Instances/v1/ontology-v20251001.json" };
// foreach (var ontologyPath in ontologyInstancePaths)
// {
//     var isValid = Ontology.Validate(ontologyPath);
//     Console.WriteLine($"'{ontologyPath}' JSON schema check: {(isValid ? "PASS" : "FAIL")}");
// }

Console.WriteLine("=====");
