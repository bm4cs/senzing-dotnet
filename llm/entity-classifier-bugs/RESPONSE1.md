Below is a concrete way to fix **both problems** while keeping your original “record movement is the source of truth” philosophy — but tightening it so:

1. **A StableId is 1:1 with a single current Senzing entity cluster** (never many).
2. **Splits are detected even when Senzing reuses the original entityId**, because you classify based on **where records went**, not whether the entityId vanished.

I’m going to show **drop-in rewrites** for both classes, plus the **minimal repository surface changes** needed.

---

## Fix 1: SenzingEventProcessor — classify splits/merges by record movement (even when entityId persists)

Your current classification only considers “SplitInto” when `didEntityExistBefore && !doesEntityExistNow`, which misses the common “entity survives but loses records to new entity” case you described.

### Key change

Build both:

- `preByRecord`: RecordId → entityIds that owned it **before**
- `postByRecord`: RecordId → entityIds that own it **now**

Then for an entity that still exists:

- if it **lost** records and those records appear in other post entities → `SplitInto`
- if it **gained** records that previously belonged to other pre entities → “merge-ish”; keep `Changed` unless you add a new enum
- `Grow` only when gained records were **new** (not in `preByRecord`)
- `Shrink` only when lost records are **gone** (not in `postByRecord`)

### Updated `SenzingEventProcessor.cs`

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
        if (affectedEntityIds.Count == 0) return [];

        // 1) Pre-state from *our* DB
        var preExists = new Dictionary<long, bool>();
        var preRecords = new Dictionary<long, List<RecordId>>();

        foreach (long entityId in affectedEntityIds)
        {
            bool existed = await _stateRepo.WasKnownToExist(entityId);
            preExists[entityId] = existed;

            var records = (await _stateRepo.GetKnownRecords(entityId))?.ToList() ?? [];
            preRecords[entityId] = records;
        }

        // 2) Post-state from Senzing
        var postExists = new Dictionary<long, bool>();
        var postRecords = new Dictionary<long, List<RecordId>>();

        foreach (long entityId in affectedEntityIds)
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

        // 3) Build preByRecord and postByRecord for movement analysis
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

        // 4) For each old entity, where did its old records go?
        //    nextEntities[oldEntity] includes self if some records remain.
        var nextEntities = new Dictionary<long, HashSet<long>>();
        var deletedRecords = new Dictionary<long, List<RecordId>>();

        foreach (long entityId in affectedEntityIds)
        {
            bool existedBefore = preExists.TryGetValue(entityId, out var ex) && ex;
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

        // 5) contributorsByCurrent: currentEntity -> {oldEntityIds that fed into it}
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

        // 6) Classify
        var results = new List<EntityChangeSummary>();

        foreach (long entityId in affectedEntityIds)
        {
            bool existedBefore = preExists.TryGetValue(entityId, out var b) && b;
            bool existsNow = postExists.TryGetValue(entityId, out var n) && n;

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
                // The entityId itself is gone. Distinguish: death vs merge vs split by where its records went.
                if (next.Count == 0)
                {
                    kind = EntityStatusType.Death;
                }
                else if (next.Count == 1)
                {
                    kind = EntityStatusType.MergeInto;
                }
                else
                {
                    kind = EntityStatusType.SplitInto;
                }
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
                    // Lost records: did they move elsewhere (=> split) or disappear (=> shrink)?
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

                    // Gained records: did they come from other prior entities (=> merge-ish) or are they brand new (=> grow)?
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

                    bool hasSplit = lostMovedTargets.Count > 0;
                    bool hasMergeish = gainedFromSources.Count > 0;
                    bool hasGrow = gainedNew.Count > 0;
                    bool hasShrink = lostDeleted.Count > 0;

                    // Prefer explicit split when it cleanly applies (your E1 example becomes SplitInto even though E1 survives)
                    if (hasSplit && !hasMergeish && !hasGrow && !hasShrink)
                        kind = EntityStatusType.SplitInto;
                    else if (hasGrow && !hasSplit && !hasMergeish && !hasShrink)
                        kind = EntityStatusType.Grow;
                    else if (hasShrink && !hasSplit && !hasMergeish && !hasGrow)
                        kind = EntityStatusType.Shrink;
                    else
                        kind = EntityStatusType.Changed;
                }
            }

            var contributors = contributorsByCurrent.TryGetValue(entityId, out var cs) ? cs : [];

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
                Contributors = contributors,
            });
        }

        // 7) Persist new snapshots
        foreach (long entityId in affectedEntityIds)
        {
            var existsNow = postExists.TryGetValue(entityId, out var ex) && ex;
            var nowRecords = postRecords.TryGetValue(entityId, out var list) ? list : [];
            await _stateRepo.SaveEntitySnapshot(entityId, existsNow, nowRecords);
        }

        return results;
    }
}
```

This directly fixes your example:

- E1 existed before and exists now, but lost R102 which now belongs to E2 ⇒ **E1 = SplitInto** (even though entityId survived).
- E2 didn’t exist before and exists now ⇒ **E2 = Birth**.

---

## Fix 2: StableIdService — enforce StableId 1:1 with a single current entity; create new StableId on split

Right now, `existingCanonicalStableIds.Count == 1 => reuse` causes the “stable points at multiple entities” bug you described.

### Minimal model change (persistent, kubernetes-safe)

Persist **one** current entity mapping per StableId:

- `StableId -> currentEntityId` (single)
- `entityId -> StableId` (single) (optional but very helpful)
- `record -> StableId`

Then implement this rule:

> If a record’s StableId is currently assigned to _another_ entityId, **do not reuse it**.
> Mint a new StableId for this entity, and record a “split lineage” link from the old StableId to the new one.

### Required repository additions

Add these to `IStableIdRepository` (names are suggestions):

```csharp
Task<EntityId?> GetStableIdForEntityAsync(long entityId);
Task SetStableIdForEntityAsync(long entityId, EntityId stableId);

Task<long?> GetEntityIdForStableAsync(EntityId stableId);
Task SetEntityIdForStableAsync(EntityId stableId, long entityId);

// Optional but recommended so your API can report split alternatives to callers:
Task AddSplitChildAsync(EntityId parentStableId, EntityId childStableId);
Task<IReadOnlyList<EntityId>> GetSplitChildrenAsync(EntityId stableId);
```

### Updated `StableIdService.cs` (also fixes the compile issues in your pasted file)

```csharp
using EntityResolutionSource.Senzing.Domain;
using EntityResolutionSource.Senzing.Interfaces;

namespace EntityResolutionSource.Senzing.Impl;

/// <summary>
/// Stable external ID layer on top of Senzing.
/// Enforces: ONE StableId <-> ONE current Senzing entity cluster.
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
        var seen = new HashSet<EntityId>();

        while (true)
        {
            if (!seen.Add(stableId))
                throw new InvalidOperationException($"Cycle detected in StableId aliases at {stableId}");

            var canonical = await _stableIdRepository.GetCanonicalIdAsync(stableId);

            if (canonical == null || canonical == stableId)
                return stableId;

            stableId = canonical;
        }
    }

    /// <summary>
    /// Assign (or create) the StableId that represents THIS entity cluster NOW.
    ///
    /// Rules:
    /// - If entity already has a StableId => reuse it.
    /// - Else if records indicate multiple StableIds => merge (canonicalize losers).
    /// - Else if records indicate a single StableId:
    ///     - if that StableId is already assigned to a different entity => split => mint new StableId and link lineage.
    ///     - otherwise reuse it.
    /// - Always: StableId maps to ONE entityId (current).
    /// </summary>
    public async Task<EntityId> UpsertStableIdForEntityAsync(long entityId, IReadOnlyCollection<RecordId> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        // 0) If this entity already has a stable, that's the natural anchor.
        var existingForEntity = await _stableIdRepository.GetStableIdForEntityAsync(entityId);
        if (existingForEntity != null)
            existingForEntity = await ResolveCanonicalStableIdAsync(existingForEntity);

        // 1) Discover canonical stable IDs referenced by these records.
        var canonicalFromRecords = new HashSet<EntityId>();
        foreach (var record in records)
        {
            var stable = await _stableIdRepository.GetStableIdForRecordAsync(record);
            if (stable == null) continue;

            canonicalFromRecords.Add(await ResolveCanonicalStableIdAsync(stable));
        }

        // If entity already has stable, treat it as a candidate even if none of the records are yet mapped.
        if (existingForEntity != null)
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

            // Enforce 1:1 mapping: if candidate is currently "owned" by another entity -> split.
            var assignedEntity = await _stableIdRepository.GetEntityIdForStableAsync(candidate);

            if (assignedEntity == null || assignedEntity == entityId)
            {
                survivor = candidate;
            }
            else
            {
                // SPLIT: candidate stays with its existing entity; this cluster gets a new stable.
                survivor = EntityId.New;
                await _stableIdRepository.CreateStableAsync(survivor);

                // Optional lineage so clients asking for candidate can be told about the new alternative.
                await _stableIdRepository.AddSplitChildAsync(candidate, survivor);
            }
        }
        else
        {
            // MERGE: multiple stable IDs now appear in the same current entity cluster.
            survivor = PickSurvivor(canonicalFromRecords);

            foreach (var loser in canonicalFromRecords)
            {
                if (loser == survivor) continue;
                await _stableIdRepository.SetCanonicalIdAsync(loser, survivor);
            }
        }

        // 2) Ensure all records in this entity cluster point to the survivor stable.
        foreach (var record in records)
            await _stableIdRepository.SetStableIdForRecordAsync(record, survivor);

        // 3) Persist 1:1 mapping (current pointers)
        await _stableIdRepository.SetStableIdForEntityAsync(entityId, survivor);
        await _stableIdRepository.SetEntityIdForStableAsync(survivor, entityId);

        return survivor;
    }

    private static EntityId PickSurvivor(IEnumerable<EntityId> candidates)
    {
        // Deterministic: lexicographic by string form (avoid assuming EntityId implements IComparable)
        return candidates
            .OrderBy(c => c.ToString(), StringComparer.Ordinal)
            .First();
    }

    /// <summary>
    /// A "simple" resolve: canonical stable + current entityId (single).
    /// </summary>
    public async Task<(EntityId canonicalStableId, long? entityId)> ResolveAsync(EntityId externalStableId)
    {
        var canonical = await ResolveCanonicalStableIdAsync(externalStableId);
        var entityId = await _stableIdRepository.GetEntityIdForStableAsync(canonical);
        return (canonical, entityId);
    }

    /// <summary>
    /// Optional richer resolve: also return split descendants so callers can choose.
    /// </summary>
    public async Task<(EntityId canonicalStableId, long? entityId, IReadOnlyList<EntityId> splitChildren)> ResolveDetailedAsync(
        EntityId externalStableId)
    {
        var canonical = await ResolveCanonicalStableIdAsync(externalStableId);
        var entityId = await _stableIdRepository.GetEntityIdForStableAsync(canonical);
        var children = await _stableIdRepository.GetSplitChildrenAsync(canonical);
        return (canonical, entityId, children);
    }
}
```

### What this changes in your split scenario

- Before: E1 (SI001) splits and creates E2, but E2 reuses SI001 ⇒ **SI001 points to E1 and E2** (bad).
- After: When processing E2:

  - candidate stable = SI001
  - SI001 is assigned to E1 (different) ⇒ **mint SI002**, link SI001 → SI002, and map E2 → SI002.

So SI001 remains “the stable for the surviving cluster” (E1), and SI002 is the new stable for the split-out cluster (E2). No stable points to multiple entities.

---

## Small orchestration note (important)

To avoid order-dependence when you process multiple affected entities, use this pattern:

1. Call `ProcessAffectedEntities(affectedEntityIds)`
2. For every summary where `PostExists == true`, call `UpsertStableIdForEntityAsync(summary.EntityId, summary.PostRecords)`
3. (Optional) For `Status == MergeInto` where you know the destination entity, you can choose to also **canonicalize the stable IDs** at that time — but the “merge via co-resident records” path above will typically catch it anyway.

---

If you want, paste your `IStableIdRepository` and `EntityId / RecordId` types (or point me to their files) and I’ll tailor the signatures precisely (and ensure the ordering / determinism matches your domain types).
