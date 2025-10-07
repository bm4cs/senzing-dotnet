using System.Net.Http.Headers;
using System.Reflection;
using SzConnyApp.Ontology;

Console.WriteLine("ConnyApp v0.1");

var ontologyInstancePaths = new[]
{
    "Ontology/Instances/v1/ems-ontology-v20251001-sample.json",
    // "Ontology/Instances/v1/ems-ontology-v20251001-simple.json",
};

foreach (var ontologyPath in ontologyInstancePaths)
{
    var isValid = Ontology.Validate(ontologyPath);
    Console.WriteLine($"'{ontologyPath}' JSON schema check: {(isValid ? "PASS" : "FAIL")}");
}

Console.WriteLine(Directory.GetCurrentDirectory());
