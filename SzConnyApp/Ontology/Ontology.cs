using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace SzConnyApp.Ontology;

internal abstract class Ontology
{
    private static JSchema OntologySchema
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = @"SzConnyApp.ontology-v20251001.schema.json";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var result = reader.ReadToEnd();
            return JSchema.Parse(result);
        }
    }

    public static bool Validate(string document)
    {
        JSchema schema = Ontology.OntologySchema;
        JObject ontology = JObject.Parse(document);
        IList<ValidationError> errors = new List<ValidationError>();
        bool valid = ontology.IsValid(schema, out errors);

        foreach (var error in errors)
        {
            Console.WriteLine(error.Message);
        }
        
        return valid;
    }
}
