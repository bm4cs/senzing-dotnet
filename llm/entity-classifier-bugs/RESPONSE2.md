Cool — with your actual types/interfaces in hand, here are **concrete, compile-clean** replacements that (a) fix the split/merge misclassification, and (b) enforce **StableId ⇄ single current Senzing entity** (no more “one stable points to many entities”).

I’ll give you:

1. A **full replacement** `SenzingEventProcessor.cs` (no more “splits only when entityId disappears”)
2. A **full replacement** `StableIdService.cs` that:

   - fixes the typos/compile issues in your current file
   - stops reusing the same StableId across split-out clusters

3. A **minimal extension** to `IStableIdRepository` to support **split lineage** (so your API can tell clients: “SI001 split; here are SI002/SI003 alternatives”)

Your current repository contracts are here: and your domain IDs are:

---

## 1) Replace `SenzingEventProcessor.cs` (movement-based classification)

**Key behavior change:**
For an entity that _still exists_ but _lost records_, we check whether those lost records now appear in other entities. If yes ⇒ `SplitInto`. If they’re gone ⇒ `Shrink`.

```csharp
using EntityResolutionSource.Senzing.Domain;
using EntityResolutionSource.Senzing.Interfaces;
using Senzing.Sdk;

namespace EntityResolutionSource.Senzing.Impl;

/// <summary>
/// Wraps SzEngine and classifies changes by *record movement*
/// (works even when Senzing reuses entity IDs during splits/merges).
/// </summary>
public sealed class SenzingEventProcessor : IEntityResolutionEngineEventProcessor
{
    private readonly ISzEnvironmentWrapper _szEnvironment;
    private readonly IEntityStateRepository _stateRepo;

    public SenzingEventProcessor(ISzEnvironmentWrapper szEnvironment, IEntityStateRepository stateRepo)
    {
        _szEnvironment = szEnvironment;
        _stateRepo = stateRepo;
    }

    public async Task<IReadOnlyList<EntityChangeSummary>> ProcessAffectedEntities(
        IReadOnlyCollection<long> affectedEntityIds)
    {
        if (affectedEntityIds is null) throw new ArgumentNullException(nameof(affectedEntityIds));
        if (affectedEntityIds.Count == 0) return [];

        // 1) Pre-state from our DB
        var preExists = new Dictionary<long, bool>(affectedEntityIds.Count);
        var preRecords = new Dictionary<long, List<RecordId>>(affectedEntityIds.Count);

        foreach (var entityId in affectedEntityIds)
        {
            var existed = await _stateRepo.WasKnownToExist(entityId);
            preExists[entityId] = existed;

            var records = (await _stateRepo.GetKnownRecords(entityId))?.ToList() ?? [];
            preRecords[entityId] = records;
        }

        // 2) Post-state from Senzing
        var postExists = new Dictionary<long, bool>(affectedEntityIds.Count);
        var postRecords = new Dictionary<long, List<RecordId>>(affectedEntityIds.Count);

        foreach (var entityId in affectedEntityIds)
        {
            try
            {
                var entity = await _szEnvironment.GetEntityV4Async(entityId);

                var recordIds = entity.ResolvedEntity.Records
                    .Select(r => new RecordId(r.RecordId))
                    .ToList();

                postExists[entityId] = true;
                postRecords[entityId] = recordIds;
            }
            catch (SzNotFoundException)
            {
                postExists[entityId] = false;
                postRecords[entityId] = [];
            }
        }

        // 3) Build preByRecord and postByRecord (RecordId -> owning entity IDs)
        var preByRecord = new Dictionary<RecordId, HashSet<long>>();
        foreach (var (entityId, records) in preRecords)
        {
            if (!(preExists.TryGetValue(entityId, out var ex) && ex)) continue;

            foreach (var record in records)
            {
                if (!preByRecord.TryGetValue(record, out var set))
                {
                    set = [];
                    preByRecord[record] = set;
                }
                set.Add(entityId);
            }
        }

        var postByRecord = new Dictionary<RecordId, HashSet<long>>();
        foreach (var (entityId, records) in postRecords)
        {
            if (!(postExists.TryGetValue(entityId, out var ex) && ex)) continue;

            foreach (var record in records)
            {
                if (!postByRecord.TryGetValue(record, out var set))
                {
                    set = [];
                    postByRecord[record] = set;
                }
                set.Add(entityId);
            }
        }

        // 4) For each old entity: where did its old records go now?
        //    nextEntities[oldEntity] contains the set of post entities that own its old records.
        var nextEntities = new Dictionary<long, HashSet<long>>();
        var deletedRecords = new Dictionary<long, List<RecordId>>();

        foreach (var entityId in affectedEntityIds)
        {
            var existedBefore = preExists.TryGetValue(entityId, out var eb) && eb;
            if (!existedBefore) continue;

            var oldRecords = preRecords[entityId];
            var nextSet = new HashSet<long>();
            var deletedList = new List<RecordId>();

            foreach (var record in oldRecords)
            {
                if (postByRecord.TryGetValue(record, out var ownersNow) && ownersNow.Count > 0)
                {
                    foreach (var owner in ownersNow)
                        nextSet.Add(owner);
                }
                else
                {
                    deletedList.Add(record);
                }
            }

            if (nextSet.Count > 0) nextEntities[entityId] = nextSet;
            if (deletedList.Count > 0) deletedRecords[entityId] = deletedList;
        }

        // 5) Contributors: for each *current* entity, which old entities fed into it
        var contributorsByCurrent = new Dictionary<long, HashSet<long>>();
        foreach (var (oldEntityId, nextSet) in nextEntities)
        {
            foreach (var currentEntityId in nextSet)
            {
                if (!contributorsByCurrent.TryGetValue(currentEntityId, out var set))
                {
                    set = [];
                    contributorsByCurrent[currentEntityId] = set;
                }
                set.Add(oldEntityId);
            }
        }

        // 6) Classify each affected entity
        var results = new List<EntityChangeSummary>(affectedEntityIds.Count);

        foreach (var entityId in affectedEntityIds)
        {
            var existedBefore = preExists.TryGetValue(entityId, out var b) && b;
            var existsNow = postExists.TryGetValue(entityId, out var n) && n;

            var before = preRecords.TryGetValue(entityId, out var br) ? br : [];
            var after = postRecords.TryGetValue(entityId, out var ar) ? ar : [];

            var next = nextEntities.TryGetValue(entityId, out var ns) ? ns : [];
            var deleted = deletedRecords.TryGetValue(entityId, out var dr) ? dr : [];

            var kind = EntityStatusType.Unknown;

            if (!existedBefore && existsNow)
            {
                kind = EntityStatusType.Birth;
            }
            else if (existedBefore && !existsNow)
            {
                // EntityId disappeared. Decide death/merge/split by where its records went.
                if (next.Count == 0) kind = EntityStatusType.Death;
                else if (next.Count == 1) kind = EntityStatusType.MergeInto;
                else kind = EntityStatusType.SplitInto;
            }
            else if (existedBefore && existsNow)
            {
                var oldSet = new HashSet<RecordId>(before);
                var newSet = new HashSet<RecordId>(after);

                var lost = oldSet.Except(newSet).ToList();
                var gained = newSet.Except(oldSet).ToList();

                if (lost.Count == 0 && gained.Count == 0)
                {
                    kind = EntityStatusType.Unchanged;
                }
                else
                {
                    // Lost -> moved? (split) or gone? (shrink)
                    var lostMovedTargets = new HashSet<long>();
                    var lostDeleted = new List<RecordId>();

                    foreach (var record in lost)
                    {
                        if (postByRecord.TryGetValue(record, out var ownersNow))
                        {
                            foreach (var owner in ownersNow)
                                if (owner != entityId) lostMovedTargets.Add(owner);
                        }
                        else
                        {
                            lostDeleted.Add(record);
                        }
                    }

                    // Gained -> from others? (merge-ish) or new? (grow)
                    var gainedFromSources = new HashSet<long>();
                    var gainedNew = new List<RecordId>();

                    foreach (var record in gained)
                    {
                        if (preByRecord.TryGetValue(record, out var ownersBefore))
                        {
                            foreach (var owner in ownersBefore)
                                if (owner != entityId) gainedFromSources.Add(owner);
                        }
                        else
                        {
                            gainedNew.Add(record);
                        }
                    }

                    var hasSplit = lostMovedTargets.Count > 0;
                    var hasMergeish = gainedFromSources.Count > 0;
                    var hasGrow = gainedNew.Count > 0;
                    var hasShrink = lostDeleted.Count > 0;

                    // If it’s a pure split signal (your E1 example), call it SplitInto even though entityId survives.
                    if (hasSplit && !hasMergeish && !hasGrow && !hasShrink) kind = EntityStatusType.SplitInto;
                    else if (hasGrow && !hasSplit && !hasMergeish && !hasShrink) kind = EntityStatusType.Grow;
                    else if (hasShrink && !hasSplit && !hasMergeish && !hasGrow) kind = EntityStatusType.Shrink;
                    else kind = EntityStatusType.Changed;
                }
            }

            var contributorSet = contributorsByCurrent.TryGetValue(entityId, out var cs) ? cs : [];

            results.Add(new EntityChangeSummary
            {
                EntityId = entityId,
                PreExists = existedBefore,
                PostExists = existsNow,
                PreRecords = before,
                PostRecords = after,
                NextEntities = next,
                DeletedRecords = deleted,
                Status = kind,
                Contributors = contributorSet,
            });
        }

        // 7) Persist new snapshots
        foreach (var entityId in affectedEntityIds)
        {
            var existsNow = postExists.TryGetValue(entityId, out var exNow) && exNow;
            var nowRecords = postRecords.TryGetValue(entityId, out var list) ? list : [];
            await _stateRepo.SaveEntitySnapshot(entityId, existsNow, nowRecords);
        }

        return results;
    }
}
```

---

## 2) Extend `IStableIdRepository` to support split lineage + 1:1 “current pointer”

Right now you only have “StableId -> EntityIds” as a _set_ , which is the root of Problem 1 (stable pointing at multiple entities). We’ll keep those methods for backwards compatibility, but add a **single current entity pointer** and **split-children lineage**.

Add these members to `IStableIdRepository`:

```csharp
// 1:1 current pointer (preferred over the plural mapping)
Task<long?> GetCurrentEntityIdForStableAsync(EntityId canonicalStableId);
Task SetCurrentEntityIdForStableAsync(EntityId canonicalStableId, long entityId);

// Optional but very useful: reverse pointer
Task<EntityId?> GetCurrentStableIdForEntityAsync(long entityId);
Task SetCurrentStableIdForEntityAsync(long entityId, EntityId canonicalStableId);

// Split lineage (so API can tell clients what SI001 split into)
Task AddSplitChildAsync(EntityId parentCanonicalStableId, EntityId childCanonicalStableId);
Task<IReadOnlyCollection<EntityId>> GetSplitChildrenAsync(EntityId parentCanonicalStableId);
```

> You can implement these in your DB as simple tables:
>
> - `StableCurrent(canonicalStableId PK, currentEntityId)`
> - `EntityCurrent(entityId PK, canonicalStableId)`
> - `StableSplit(parentStableId, childStableId)` with `(parent, child)` unique.

---

## 3) Replace `StableIdService.cs` (enforce stable ⇄ single entity + split minting)

Your current file also has a few **compile errors** (`SetStableIdForRecordA...`, `ImmutableList<long>.Creeate`, `Seq<long>`) — this replacement fixes those and implements the split rule.

```csharp
using EntityResolutionSource.Senzing.Domain;
using EntityResolutionSource.Senzing.Interfaces;

namespace EntityResolutionSource.Senzing.Impl;

/// <summary>
/// Stable external ID layer on top of Senzing.
/// Enforces: ONE canonical StableId <-> ONE current Senzing entity cluster.
/// Merges => canonical aliasing. Splits => mint new StableId(s) and keep lineage.
/// </summary>
public sealed class StableIdService : IStableIdService
{
    private readonly IStableIdRepository _stableIdRepository;

    public StableIdService(IStableIdRepository stableIdRepository)
    {
        _stableIdRepository = stableIdRepository;
    }

    public async Task<EntityId> ResolveCanonicalStableIdAsync(EntityId stableId)
    {
        if (stableId is null) throw new ArgumentNullException(nameof(stableId));

        var seen = new HashSet<EntityId>();

        while (true)
        {
            if (!seen.Add(stableId))
                throw new InvalidOperationException($"Cycle detected in StableId canonical chain at {stableId}");

            var canonical = await _stableIdRepository.GetCanonicalIdAsync(stableId);

            // canonical == null means either canonical or missing row; treat as stableId itself.
            if (canonical is null || canonical == stableId)
                return stableId;

            stableId = canonical;
        }
    }

    public async Task<EntityId> UpsertStableIdForEntityAsync(long entityId, IReadOnlyCollection<RecordId> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        // 0) If this entity already has a known stable ID, prefer it as the anchor.
        EntityId? existingForEntity = null;
        try
        {
            existingForEntity = await _stableIdRepository.GetCurrentStableIdForEntityAsync(entityId);
            if (existingForEntity is not null)
                existingForEntity = await ResolveCanonicalStableIdAsync(existingForEntity);
        }
        catch (NotImplementedException)
        {
            // If you haven't added the new repo methods yet, this stays null.
        }

        // 1) Discover canonical stable IDs referenced by these records.
        var canonicalFromRecords = new HashSet<EntityId>();

        foreach (var record in records)
        {
            var stable = await _stableIdRepository.GetStableIdForRecordAsync(record);
            if (stable is null) continue;
            canonicalFromRecords.Add(await ResolveCanonicalStableIdAsync(stable));
        }

        if (existingForEntity is not null)
            canonicalFromRecords.Add(existingForEntity);

        EntityId survivor;

        if (canonicalFromRecords.Count == 0)
        {
            survivor = EntityId.New;
            await _stableIdRepository.CreateStableAsync(survivor);
        }
        else if (canonicalFromRecords.Count == 1)
        {
            var candidate = canonicalFromRecords.First();

            // SPLIT DETECTION: if candidate stable is already "currently pointing at"
            // a different entity cluster, we MUST mint a new stable for this entity.
            long? assignedEntity = null;
            try
            {
                assignedEntity = await _stableIdRepository.GetCurrentEntityIdForStableAsync(candidate);
            }
            catch (NotImplementedException)
            {
                // If you haven't added the new method yet, fall back to legacy plural mapping.
                var legacy = await _stableIdRepository.GetEntityIdsForStableAsync(candidate);
                assignedEntity = legacy.Count == 1 ? legacy.First() : null;
            }

            if (assignedEntity is null || assignedEntity == entityId)
            {
                survivor = candidate;
            }
            else
            {
                // Split-out cluster: mint a new stable and record lineage.
                survivor = EntityId.New;
                await _stableIdRepository.CreateStableAsync(survivor);

                try
                {
                    await _stableIdRepository.AddSplitChildAsync(candidate, survivor);
                }
                catch (NotImplementedException)
                {
                    // If lineage isn't implemented yet, you still get correctness (1:1),
                    // you just won't be able to enumerate alternatives for clients.
                }
            }
        }
        else
        {
            // MERGE: multiple stable IDs now appear in one entity cluster.
            survivor = PickDeterministicSurvivor(canonicalFromRecords);

            foreach (var loser in canonicalFromRecords)
            {
                if (loser == survivor) continue;
                await _stableIdRepository.SetCanonicalIdAsync(loser, survivor);
            }
        }

        // 2) Ensure all records map to the survivor stable.
        foreach (var record in records)
            await _stableIdRepository.SetStableIdForRecordAsync(record, survivor);

        // 3) Enforce 1:1 stable->current entity pointer.
        try
        {
            await _stableIdRepository.SetCurrentEntityIdForStableAsync(survivor, entityId);
            await _stableIdRepository.SetCurrentStableIdForEntityAsync(entityId, survivor);
        }
        catch (NotImplementedException)
        {
            // Back-compat: use legacy plural mapping, but WRITE IT AS A SINGLETON.
            await _stableIdRepository.SetEntityIdsForStableAsync(survivor, new List<long> { entityId });
        }

        return survivor;
    }

    private static EntityId PickDeterministicSurvivor(IEnumerable<EntityId> candidates)
        => candidates.OrderBy(c => c.entityId, StringComparer.Ordinal).First();

    /// <summary>
    /// Basic resolve: returns canonical stable + the single current entityId (if any).
    /// </summary>
    public async Task<(EntityId canonicalStableId, IReadOnlyCollection<long> entityIds)> ResolveAsync(EntityId externalStableId)
    {
        var canonical = await ResolveCanonicalStableIdAsync(externalStableId);

        // Prefer new single-pointer if you added it; otherwise use legacy mapping.
        try
        {
            var entityId = await _stableIdRepository.GetCurrentEntityIdForStableAsync(canonical);
            return (canonical, entityId is null ? Array.Empty<long>() : new[] { entityId.Value });
        }
        catch (NotImplementedException)
        {
            var entityIds = await _stableIdRepository.GetEntityIdsForStableAsync(canonical);
            return (canonical, entityIds);
        }
    }

    /// <summary>
    /// Rich resolve: returns canonical stable + current entity + split alternatives (stableIds).
    /// Your API can then resolve each child stableId to its entity.
    /// </summary>
    public async Task<(EntityId canonicalStableId, long? currentEntityId, IReadOnlyCollection<EntityId> splitChildren)>
        ResolveDetailedAsync(EntityId externalStableId)
    {
        var canonical = await ResolveCanonicalStableIdAsync(externalStableId);

        long? current = null;
        try
        {
            current = await _stableIdRepository.GetCurrentEntityIdForStableAsync(canonical);
        }
        catch (NotImplementedException)
        {
            var legacy = await _stableIdRepository.GetEntityIdsForStableAsync(canonical);
            current = legacy.Count == 1 ? legacy.First() : null;
        }

        IReadOnlyCollection<EntityId> children = Array.Empty<EntityId>();
        try
        {
            children = await _stableIdRepository.GetSplitChildrenAsync(canonical);
        }
        catch (NotImplementedException)
        {
            // lineage not available
        }

        return (canonical, current, children);
    }
}
```

### Why this fixes your Problem 1

That old block:

```csharp
else if (existingCanonicalStableIds.Count == 1) survivor = existingCanonicalStableIds.First();
```

…is now guarded: if that candidate stable is already the “current pointer” for some _other_ entity, we **mint a new stable** instead (split), and we never add multiple entityIds to a single stable.

---

## One small note about your `EntityId.New`

Right now `EntityId.New` produces `"E-{Guid}"` . If this type is being used for **StableIds**, you may want to change the prefix to `"SI-"` or similar — purely cosmetic, but it’ll save confusion later.

---

If you want, paste your current `IStableIdService` signature (and/or how your API endpoint returns stable resolution) and I’ll show exactly how to return:

- canonical stable
- current entity
- split alternatives as `(stableId, entityId)` tuples

…in one call, without extra round trips.
