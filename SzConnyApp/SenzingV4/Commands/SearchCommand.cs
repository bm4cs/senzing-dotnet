using System.Text.Json;
using Microsoft.Extensions.Logging;
using Senzing.Typedef;
// using Newtonsoft.Json;
using SzConnyApp.SenzingV4.Senzing;
using static Senzing.Sdk.SzFlags;

namespace SzConnyApp.SenzingV4.Commands;

public class SearchCommand(ISzEnvironmentWrapper szEnvironment, ILogger<RecordLoaderCommand> logger)
    : ISearchCommand
{
    public void Execute()
    {
        var szEngine = szEnvironment.Engine;
        foreach (var searchCriteria in GetSearchCriteria())
        {
            var jsonString = szEngine.SearchByAttributes(searchCriteria, SzSearchByAttributesDefaultFlags);
            var searchResponse = JsonSerializer.Deserialize<SzEngineSearchByAttributesResponse>(jsonString);
            logger.LogInformation($"Search hits: {searchResponse.ResolvedEntities.Count}");
        }
    }

    private static IList<string> GetSearchCriteria()
    {
        var records = new List<string>();
        records.Add(
            """
            {
                "NAME_FULL": "Susan Moony",
                "DATE_OF_BIRTH": "15/6/1998",
                "SSN_NUMBER": "521212123"
            }
            """);

        records.Add(
            """
            {
                "NAME_FIRST": "Robert",
                "NAME_LAST": "Smith",
                "ADDR_FULL": "123 Main Street Las Vegas NV 89132"
            }
            """);

        records.Add(
            """
            {
                "NAME_FIRST": "Makio",
                "NAME_LAST": "Yamanaka",
                "ADDR_FULL": "787 Rotary Drive Rotorville FL 78720"
            }
            """);

        return records;
    }
}
