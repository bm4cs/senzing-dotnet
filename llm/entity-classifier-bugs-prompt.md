I've been running some Senzing (for entity resolution) experiements from a .NET codebase.

I have built 2 classes (`SenzingEventProcessor.cs` and `StableIdService.cs`) to track the unstable entity problem, based on the design outlined in 2025-12-03-entity-resolution-simple-stability.md

I've identified a few problems with the implementation, which I want your assistance to rectify.

## Problem 1: StableIdService reuses StableId when Senzing splits some of the records off a pre-existing entity cluster to another new entity cluster

Line 96 in `StableIdService.cs` has the following logic:

```csharp
else if (existingCanonicalStableIds.Count == 1)
{
    // All records agree on a single canonical StableId.
    survivor = existingCanonicalStableIds.First();
}
```

The StableIdService assumes that if records were already associated with a single pre-existing StableId, they should stick with the StableId, even if some of the records "split out" into other new Senzing entity clusters. The StableIdService, keeps the records associated with the original StableId the "split out" records were associated with, and registers the internal Senzing entity ID's with the StableId. In effect, mixing different Senzing entity clusters together.

Why this is a problem:

- This is undesired, as the StableId now in effect point to multiple distinct Senzing entity ID's that represent completely different entities (e.g. 'Bob Smith' and 'Mary Jones').
- The code assumes that Senzing split events result in new entity ID's. However, Senzing can and does reuse entity ID's. For example, assume that Senzing entity E1 contains records R100, R101, and R102. E1 is associated to StableId `SI001`. Now record R102 gets updated, adding more information about the person it represents. This causes Senzing to report affected entities [E1, E2]. On deeper inspection with Senzing (by running GetEntity for E1 and E2), reports that E1 => [R100, R101], E2 => [R102]. Note, how entity E1 still lives on in Senzing, however has experienced a reduction in records associated with it. The `StableIdService` will classify this as a `EntityStatusType.Shrink`, which I consider another bug with the design/implementation, as this should be a `EntityStatusType.SplitInto`.

Desired behaviour (weaknesses in current design):

- The ability to handle updates to existing Records, which may alter zero, one or more entity clusters.
- A StableEntityId should only relate to a single underlying Senzing entity cluster - currently it supports being linked to multiple Senzing entities. In other words it doesnt make sense to tie multiple distinct Senzing entity clusters together, as they each represent different concepts (people, places, things).
- As Senzing records lifecycle to new and/or different entity clusters, instead of associating the StableEntityId to multiple entity clusters (current design), the new Senzing entities should break out into new StableEntityIds. If an external API consumer of our system comes with an old stale StableEntityId (e.g. `SI001`), whos records have now "split out" to new Senzing entity clusters (and StableEntityIds to track them - e.g. `SI002`), the API should put the decision on which entity is most applicable onto the consumer, by returning the fact that while you asked for

## Problem 2: SenzingEventProcessor misclassifying Senzing entity lifecyles

The primary responsiblity of the SenzingEventProcessor, is to take a list of Senzing affected entity ID's that were impacted by an `AddRecord` call, which can either register new records or update existing records if the provided RECORD_ID already exists, and observe what occured for each affected entity. It achieves this by comparing the previous known state of Senzing entities and their associated records, with the current state.

Why this is a problem:

- The code assumes that Senzing split events result in new entity ID's. However, Senzing can and does reuse entity ID's. For example, assume that Senzing entity E1 contains records R100, R101, and R102. E1 is associated to StableId `SI001`. Now record R102 gets updated, adding more information about the person it represents. This causes Senzing to report affected entities [E1, E2]. On deeper inspection with Senzing (by running GetEntity for E1 and E2), reports that E1 => [R100, R101], E2 => [R102]. Note, how entity E1 still lives on in Senzing, however has experienced a reduction in records associated with it. The `StableIdService` will classify entity E1 as a `EntityStatusType.Shrink`, and entity E2 as a `EntityStatusType.Birth`. I consider this a bug with the design/implementation, and should classify entity E1 as a `EntityStatusType.SplitInto` (communicating back to the caller that E2 is one of the split into entities), and entity E2 as a `EntityStatusType.Birth`
- Most of the bugs in the classification logic (in `ProcessAffectedEntities()`) is clear. For example, the `EntityStatusType.SplitInto` classification if housed within the following conditional, which begins by checking `(didEntityExistBefore && !doesEntityExistNow)`. As noted above, Senzing splits do not necessarily result in the death of existing known entity clusters, its completely valid for Senzing to retain/reuse an existing entity cluster, while break a subset of its previously assoicated records out into a new entity cluster.

```csharp
else if (didEntityExistBefore && !doesEntityExistNow)
{
    // Entity ID disappeared
    if (nextEntitiesForEntity.Count == 0)
    {
        // No surviving records anywhere
        kind = EntityStatusType.Death;
    }
    else if (nextEntitiesForEntity.Count == 1)
    {
        // All surviving records moved into a single entity
        kind = EntityStatusType.MergeInto;
    }
    else
    {
        // Surviving records fanned out across multiple entities
        kind = EntityStatusType.SplitInto;
    }
}~
```

- Extending the above thinking, I feel like the `EntityStatusType.Grow` and `EntityStatusType.Shrink` classifications are weak, and in practice can mostly be replaced by `EntityStatusType.MergeInto` and/or `EntityStatusType.SplitInto`. The only caveat I can think of is that if one or more records have been deleted, that would consistute a `EntityStatusType.Shrink`. The Shrink and SplitInto logic should be very similar, in that an existing entity cluster has lost records, however if those records have been assigned to other entity clusters, then its a SplitInto, otherwise if those records are no longer, then its a Shrink. `EntityStatusType.Grow` still has a place, but only if the entity grew becuase of the addition of new records, otherwise, growths could only otherwise be caused by merges and/or splits.
