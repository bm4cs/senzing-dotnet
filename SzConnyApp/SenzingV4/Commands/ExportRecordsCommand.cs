using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Senzing.Typedef;
using SzConnyApp.SenzingV4.Senzing;
using static Senzing.Sdk.SzFlags;

namespace SzConnyApp.SenzingV4.Commands;

public class ExportRecordsCommand(
    ISzEnvironmentWrapper szEnvironment,
    ILogger<ExportRecordsCommand> logger
) : IExportRecordsCommand
{
    public void Execute()
    {
        var szEngine = szEnvironment.Engine;
        var exportHandle = szEngine.ExportJsonEntityReport(SzExportDefaultFlags);

        try
        {
            var jsonData = szEngine.FetchNext(exportHandle);

            while (jsonData != null)
            {
                // {
                //     "RESOLVED_ENTITY" : {
                //         "ENTITY_ID" : 1,
                //         "ENTITY_NAME" : "Robert Smith",
                //         "FEATURES" : {

                var resolvedEntity = JsonNode.Parse(jsonData)?.AsObject()["RESOLVED_ENTITY"];

                var entityId = resolvedEntity?.AsObject()["ENTITY_ID"]?.GetValue<int>();

                var entityName = resolvedEntity?.AsObject()["ENTITY_NAME"]?.GetValue<string>();

                logger.LogInformation($"Found entity: {entityId} = {entityName}");

                jsonData = szEngine.FetchNext(exportHandle);
            }
        }
        finally
        {
            szEngine.CloseExportReport(exportHandle);
        }
    }
}
