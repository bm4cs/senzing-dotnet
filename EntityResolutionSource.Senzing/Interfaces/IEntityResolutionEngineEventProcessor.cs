using EntityResolutionSource.Senzing.Domain;

namespace EntityResolutionSource.Senzing.Interfaces;

public interface IEntityResolutionEngineEventProcessor
{
    Task<IReadOnlyList<EntityChangeSummary>> ProcessAffectedEntities(IReadOnlyCollection<long> affectedEntityIds);
}