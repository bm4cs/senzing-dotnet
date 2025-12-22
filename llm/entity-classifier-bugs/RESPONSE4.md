Yep — the trick is to record split lineage **at the moment you mint a new StableId for a cluster whose records previously belonged to some other StableId**.

### What “deterministic split lineage” means in practice

When you process an entity cluster **E2** with records `{R102}`:

- BEFORE you rewrite anything, you read `RecordId -> StableId` for those records.
- Canonicalize those StableIds.
- If you decide “I cannot reuse parent StableId because it already points to another entity cluster” ⇒ you mint `SI002`.
- Then you persist **Split edge(s)**: `SI001 -> SI002`.

That way:

- `SI001` remains 1:1 with the surviving cluster,
- the split-out clusters get new StableIds,
- and callers asking for `SI001` can be told “also consider `SI002`, `SI003`, …”.

---

## Repository additions (best practice for Kubernetes / concurrency)

To make this deterministic even under parallelism, add **an atomic claim** to your repo:

```csharp
/// <summary>
/// Try to claim the "current entity pointer" for a stableId.
/// Return true if stableId was unassigned OR already assigned to this entityId.
/// Return false if stableId is assigned to a different entityId.
/// MUST be implemented atomically in the DB.
/// </summary>
Task<bool> TryClaimCurrentEntityForStableAsync(EntityId canonicalStableId, long entityId);
```

And for split lineage:

```csharp
Task AddSplitChildAsync(EntityId parentCanonicalStableId, EntityId childCanonicalStableId);
Task<IReadOnlyCollection<EntityId>> GetSplitChildrenAsync(EntityId parentCanonicalStableId);
```

(Implement `AddSplitChildAsync` idempotently with a unique constraint on `(parent, child)`.)

> Note: the `...` lines in the uploaded interface files are not valid C#; I’m assuming that’s just an artifact of how they were shared.

---

## StableIdService: deterministic split parents + atomic claim

Here’s the exact logic to drop into `UpsertStableIdForEntityAsync`:

- Compute `canonicalFromRecords` **and** a frequency map (`counts`) from records → canonical stable.
- Compute `parentCandidates` deterministically:

  - sort by descending count, then by stable string

- In the “single candidate” case:

  - try to claim that stable for this entity
  - if claim fails ⇒ mint new stable and add split edges from the parent(s)

### Patch-style implementation

```csharp
public async Task<EntityId> UpsertStableIdForEntityAsync(long entityId, IReadOnlyCollection<RecordId> records)
{
    ArgumentNullException.ThrowIfNull(records);

    // 1) Read current stable assignments for these records BEFORE we mutate them.
    var counts = new Dictionary<EntityId, int>(); // canonicalStableId -> count of records
    foreach (var record in records)
    {
        var stable = await _stableIdRepository.GetStableIdForRecordAsync(record);
        if (stable is null) continue;

        var canonical = await ResolveCanonicalStableIdAsync(stable);
        counts[canonical] = counts.TryGetValue(canonical, out var c) ? c + 1 : 1;
    }

    // Canonical stable IDs present on records
    var canonicalFromRecords = new HashSet<EntityId>(counts.Keys);

    EntityId survivor;

    if (canonicalFromRecords.Count == 0)
    {
        survivor = EntityId.New;
        await _stableIdRepository.CreateStableAsync(survivor);

        // claim new stable -> this entity (should always succeed)
        await _stableIdRepository.TryClaimCurrentEntityForStableAsync(survivor, entityId);
    }
    else if (canonicalFromRecords.Count == 1)
    {
        var parent = canonicalFromRecords.First();

        // Deterministic split detection via atomic claim:
        var claimed = await _stableIdRepository.TryClaimCurrentEntityForStableAsync(parent, entityId);

        if (claimed)
        {
            survivor = parent;
        }
        else
        {
            // SPLIT: parent already points to some other entity => mint a child stable for this cluster
            survivor = EntityId.New;
            await _stableIdRepository.CreateStableAsync(survivor);

            // lineage: SI001 -> SI002
            await _stableIdRepository.AddSplitChildAsync(parent, survivor);

            // claim child -> this entity
            await _stableIdRepository.TryClaimCurrentEntityForStableAsync(survivor, entityId);
        }
    }
    else
    {
        // MERGE case: multiple stable IDs now appear in one Senzing cluster.
        // Pick a deterministic survivor (e.g. highest record count, then stableId string).
        survivor = canonicalFromRecords
            .OrderByDescending(s => counts.TryGetValue(s, out var c) ? c : 0)
            .ThenBy(s => s.entityId, StringComparer.Ordinal)
            .First();

        foreach (var loser in canonicalFromRecords)
        {
            if (loser == survivor) continue;
            await _stableIdRepository.SetCanonicalIdAsync(loser, survivor);
        }

        // For merges, it’s expected the survivor “moves” to represent the merged cluster.
        // If your TryClaim disallows moves, add a separate repo method for merge moves.
        // For now, we attempt the claim (should succeed if merge processing is ordered / consistent).
        await _stableIdRepository.TryClaimCurrentEntityForStableAsync(survivor, entityId);
    }

    // 2) Now mutate record->stable mapping (records in this cluster point to the survivor)
    foreach (var record in records)
        await _stableIdRepository.SetStableIdForRecordAsync(record, survivor);

    // 3) Legacy mapping compatibility (write singleton only)
    await _stableIdRepository.SetEntityIdsForStableAsync(survivor, new List<long> { entityId });

    return survivor;
}
```

### Why this is deterministic

- The **parent** used for lineage is the canonical stable ID already assigned to the records _before_ the update.
- The “is this a split?” decision doesn’t depend on processing order if your repo implements `TryClaimCurrentEntityForStableAsync` atomically.

---

## If you want “multi-parent split” lineage (rare, but deterministic)

If you ever mint a new stable in a scenario where records were previously spread across multiple canonical stables (e.g., data corruption or weird sequencing), you can add split edges for **all** parents in a deterministic order:

```csharp
var parents = canonicalFromRecords
    .OrderByDescending(s => counts.TryGetValue(s, out var c) ? c : 0)
    .ThenBy(s => s.entityId, StringComparer.Ordinal)
    .ToList();

foreach (var p in parents)
    await _stableIdRepository.AddSplitChildAsync(p, survivor);
```

---

## One more (important) note: merges vs splits under concurrency

For merges, you may want a second repo method that allows the “current pointer” to **move** (because merges make the old entity obsolete):

```csharp
Task MoveCurrentEntityForStableAsync(EntityId canonicalStableId, long newEntityId);
```

Then:

- use `TryClaim...` for split safety (no stealing),
- use `Move...` when your event classification says “this is a merge destination”.

If you paste your current DB implementation (or table schema) I can show the exact SQL for `TryClaim...` and `Move...` for Postgres/MSSQL, including idempotency and unique constraints.
