using System.Net.Http.Headers;
using System.Reflection;
using SzConnyApp.Ontology;

Console.WriteLine("ConnyApp v0.1");

var ontologyInstancePaths = new[]
{
    "Ontology/Instances/ems-ontology-v20251001-composite.json",
    "Ontology/Instances/ems-ontology-v20251001-simple.json",
};

foreach (var ontologyPath in ontologyInstancePaths)
{
    var isValid = Ontology.Validate(ontologyPath);
    Console.WriteLine($"'{ontologyPath}' JSON schema check: {(isValid ? "PASS" : "FAIL")}");
}

Console.WriteLine(Directory.GetCurrentDirectory());
