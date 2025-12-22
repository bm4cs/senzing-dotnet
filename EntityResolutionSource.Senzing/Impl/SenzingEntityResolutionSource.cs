using System.Collections.Immutable;
using EntityResolutionSource.Senzing.Interfaces;
using EntityResolutionSource.Senzing.Models;
using EntityResolutionSource.Senzing.Domain;
using EntityResolutionSource.Senzing.Interfaces;
using InterestingEntity = EntityResolutionSource.Senzing.Domain.InterestingEntity;
using ResolvedEntity = EntityResolutionSource.Senzing.Domain.ResolvedEntity;

namespace EntityResolutionSource.Senzing.Impl;

public class SenzingEntityResolutionSource(
    ISzEnvironmentWrapper szEnvironment
) : IEntityResolutionSource
{
    public async Task<EntityResolutionResult> ResolveAsync(RecordId recordId, RecordV4 record)
    {
        var addRecordResponse = await szEnvironment.AddRecordV4Async(record);
        ArgumentNullException.ThrowIfNull(addRecordResponse);

        var affectedExternalEntityIds = addRecordResponse.AffectedEntities.Select(e =>
            e.EntityId
        );

        var interestingEntityIds = addRecordResponse?.InterestingEntities?.Entities
            .Select(e => new InterestingEntity(e.EntityId, e.Degrees)
            ).ToImmutableList();

        return await Task.FromResult(
            new EntityResolutionResult(
                RecordId: addRecordResponse.RecordId,
                AffectedEntityIds: affectedExternalEntityIds.Select(e => (long)e).ToImmutableList(),
                InterestingEntityIds: interestingEntityIds,
                AffectedEntityChanges: new List<EntityChangeSummary>(), //TODO: wire in event processor results
                RelatedEntities: new List<ResolvedEntity>()
            )
        );
    }
}