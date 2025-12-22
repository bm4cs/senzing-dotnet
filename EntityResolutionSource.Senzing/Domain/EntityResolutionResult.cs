namespace EntityResolutionSource.Senzing.Domain;

public record EntityResolutionResult(
    string RecordId,
    IReadOnlyCollection<long> AffectedEntityIds,
    IReadOnlyCollection<InterestingEntity> InterestingEntityIds,
    IReadOnlyCollection<EntityChangeSummary> AffectedEntityChanges,
    // IReadOnlyCollection<ResolvedEntity> AffectedEntities = new(),
    IReadOnlyCollection<ResolvedEntity> RelatedEntities
);
