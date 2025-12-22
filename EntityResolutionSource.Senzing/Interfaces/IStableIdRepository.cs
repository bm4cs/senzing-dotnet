using EntityResolutionSource.Senzing.Domain;

namespace EntityResolutionSource.Senzing.Interfaces;

/// <summary>
/// CRUD around StableIds and their aliasing (canonical) relationships.
/// Abstract EventFlow data access primitives (and later MartenDB).
/// </summary>
public interface IStableIdRepository
{
    /// <summary>
    /// Create a new canonical StableId row in the DB.
    /// (Typically just inserts StableId with CanonicalId = itself or NULL.)
    /// </summary>
    Task CreateStableAsync(EntityId stableId);

    /// <summary>
    /// Returns the canonical ID for a given stableId if it's an alias;
    /// returns null if either:
    ///   - the stableId is canonical (CanonicalId is NULL or equals StableId)
    ///   - the row doesn't exist.
    /// </summary>
    Task<EntityId?> GetCanonicalIdAsync(EntityId stableId);

    /// <summary>
    /// Set stableId's CanonicalId to canonicalId (meaning:
    /// "stableId is now an alias of canonicalId").
    /// </summary>
    Task SetCanonicalIdAsync(EntityId stableId, EntityId canonicalId);

    /// <summary>
    /// Get the last known Record -> StableId
    /// </summary>
    Task<EntityId?> GetStableIdForRecordAsync(RecordId record);

    /// <summary>
    /// Sets the last known Record -> StableId
    /// </summary>
    Task SetStableIdForRecordAsync(RecordId recordId, EntityId stableId);

    /// <summary>
    /// Get StableId -> EntityIds (current Senzing entities)
    /// </summary>
    Task<IReadOnlyCollection<long>> GetEntityIdsForStableAsync(EntityId canonicalStableId);

    /// <summary>
    /// Set StableId -> EntityIds (current Senzing entities)
    /// </summary>
    Task SetEntityIdsForStableAsync(EntityId canonicalStableId, IReadOnlyCollection<long> entityIds);
}
