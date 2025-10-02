using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace SzConnyApp.Ontology;

internal abstract class Ontology
{
    private static string ReadFile(string relativePath)
    {
        using var stream = File.OpenRead($"{Directory.GetCurrentDirectory()}/{relativePath}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static JSchema OntologySchema
    {
        get
        {
            var schemaContent = ReadFile("Ontology/Schema/v1/ems-ontology-v20251001.schema.json");
            return JSchema.Parse(schemaContent);
        }
    }

    public static bool Validate(string relativePathToInstance)
    {
        var instanceContent = ReadFile(relativePathToInstance);
        JSchema schema = OntologySchema;
        JObject ontology = JObject.Parse(instanceContent);
        bool valid = ontology.IsValid(schema, out IList<ValidationError> errors);

        foreach (var error in errors)
        {
            Console.WriteLine(error.Message);
        }

        return valid;
    }
}
