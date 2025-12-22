namespace EntityResolutionSource.Senzing.Domain;

/// <summary>
/// Classification of how an entity changed during a single event.
/// </summary>
public enum EntityStatusType
{
    Unknown = 0,
    Birth, // did not exist before, exists now
    Death, // existed before, now gone, records also gone
    MergeInto, // this entity ID disappeared, records moved into one other entity
    SplitInto, // this entity ID disappeared, records fanned out to multiple entities
    Shrink, // same ID, lost records only
    Grow, // same ID, gained records only
    Changed, // same ID, both lost and gained records (partial split/merge)
    Unchanged, // same set of records
}