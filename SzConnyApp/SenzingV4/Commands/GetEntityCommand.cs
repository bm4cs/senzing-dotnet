using System.Text.Json;
using Microsoft.Extensions.Logging;
using Senzing.Typedef;
using SzConnyApp.SenzingV4.Senzing;
using static Senzing.Sdk.SzFlags;

namespace SzConnyApp.SenzingV4.Commands;

public class GetEntityCommand(ISzEnvironmentWrapper szEnvironment, ILogger<RecordLoaderCommand> logger)
    : IGetEntityCommand
{
    public long EntityId { get; set; }
    
    public void Execute()
    {
        var szEngine = szEnvironment.Engine;
        var jsonString = szEngine.GetEntity(EntityId, SzSearchByAttributesDefaultFlags);
        var entityResponse = JsonSerializer.Deserialize<SzEngineGetEntityByEntityIdResponse>(jsonString);

        if (entityResponse == null)
        {
            logger.LogWarning($"Entity ID {EntityId} not found");
            return;
        }
        
        logger.LogInformation($"Entity ID: {EntityId} found");
        logger.LogInformation($"Entity Name: {entityResponse.ResolvedEntity.EntityName}");
        
        foreach (var featurePair in entityResponse.ResolvedEntity.Features)
        {
            foreach (var featureValue in featurePair.Value)
            {
                logger.LogInformation($"Feature {featurePair.Key} = {featureValue.FeatDesc}");
            }
        }
    }
}
