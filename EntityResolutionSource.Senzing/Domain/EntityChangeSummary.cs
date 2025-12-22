namespace EntityResolutionSource.Senzing.Domain;

/// <summary>
/// Summary of how a given entity ID changed in one WithInfo event.
/// </summary>
public sealed class EntityChangeSummary
{
    public long EntityId { get; init; }

    public bool PreExists { get; init; }
    public bool PostExists { get; init; }

    public IReadOnlyList<RecordId> PreRecords { get; init; } = [];
    public IReadOnlyList<RecordId> PostRecords { get; init; } = [];

    /// <summary>
    /// For an entity that disappeared, which entity IDs now own its records.
    /// (If 0 -> delete, 1 -> merge, >1 -> split.)
    /// </summary>
    public IReadOnlyCollection<long> NextEntities { get; init; } = [];

    /// <summary>
    /// Records that used to belong to this entity but are no longer present anywhere.
    /// </summary>
    public IReadOnlyList<RecordId> DeletedRecords { get; init; } = [];

    public EntityStatusType Status { get; init; }

    /// <summary>
    /// Optional: entities that contributed records into this entity.
    /// To capture merges from the survivors.
    /// </summary>
    public IReadOnlyCollection<long> Contributors { get; init; } = [];
}