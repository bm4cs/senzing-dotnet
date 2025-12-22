using EntityResolutionSource.Senzing.Domain;

namespace EntityResolutionSource.Senzing.Interfaces;

/// <summary>
/// Abstraction for your persistence of "last known entity state".
/// </summary>
public interface IEntityStateRepository
{
    /// <summary>
    /// Returns true if we previously knew this entity ID to exist.
    /// If it's completely unknown, return false.
    /// </summary>
    Task<bool> WasKnownToExist(long entityId);

    /// <summary>
    /// Returns the last known records for this entity ID.
    /// If we have no history for it, return an empty list.
    /// </summary>
    Task<IReadOnlyCollection<RecordId>> GetKnownRecords(long entityId);

    /// <summary>
    /// Update our snapshot of an entity's state after processing an event.
    /// If exists == false, you can either delete the row or mark as not-existing.
    /// </summary>
    Task SaveEntitySnapshot(long entityId, bool exists, IReadOnlyCollection<RecordId> records);
}
