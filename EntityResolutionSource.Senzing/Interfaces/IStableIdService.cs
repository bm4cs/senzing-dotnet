using EntityResolutionSource.Senzing.Domain;

namespace EntityResolutionSource.Senzing.Interfaces;

public interface IStableIdService
{
    /// <summary>
    /// Resolve the canonical StableId given any (possibly old/alias) StableId.
    /// This just chases CanonicalId pointers in the DB until we reach a root.
    /// </summary>
    Task<EntityId> ResolveCanonicalStableIdAsync(EntityId stableId);

    /// <summary>
    /// The stable entity illusion secret sauce.
    /// Given the current records for a Senzing entity,
    /// find/create the StableId that should represent that entity.
    ///
    /// Observing Record movement avoids the headaches of needing to classify explicit merge/split labels on Senzing entities.
    ///  - Collect StableIds for all records
    ///  - Map them to their canonical forms
    ///    - If 0  => new StableId
    ///    - If 1  => reuse
    ///    - If >1 => merge them into one survivor
    ///
    /// Then:
    ///  - Assign all records to the survivor StableId
    ///  - Update StableId -> EntityId mapping
    /// </summary>
    Task<EntityId> UpsertStableIdForEntityAsync(
        long entityId,
        IReadOnlyCollection<RecordId> records
    );

    /// <summary>
    /// Surface canonical ID to outside world.
    /// given a stable ID from a client (which may be old),
    /// return the current canonical StableId and the Senzing entity IDs that represent it.
    /// </summary>
    Task<(EntityId canonicalStableId, IReadOnlyCollection<long> entityIds)> ResolveAsync(EntityId externalStableId);
}
