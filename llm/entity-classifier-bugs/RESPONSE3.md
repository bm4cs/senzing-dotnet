Here’s a clean way to extend your **service contract** so your API can return, in **one call**:

- the **canonical StableId**
- the **single current Senzing entityId** (enforced 1:1)
- the **split alternatives**, already resolved to `(stableId, entityId)` tuples

Your current `IStableIdService` only supports `ResolveAsync(stableId) -> (canonicalStableId, entityIds)` , and your current `IStableIdRepository` only supports the plural `StableId -> EntityIds` mapping , which is exactly what causes “StableId points at multiple entities” during splits.

Below is the upgrade that fixes that while keeping `ResolveAsync` intact for compatibility.

---

## 1) Add DTOs (domain-friendly, API-friendly)

Create a new file (e.g. `StableResolution.cs`):

```csharp
using EntityResolutionSource.Senzing.Domain;

namespace EntityResolutionSource.Senzing.Interfaces;

public sealed record StableAlternative(EntityId stableId, long? entityId);

public sealed record StableResolution(
    EntityId requestedStableId,
    EntityId canonicalStableId,
    long? currentEntityId,
    IReadOnlyCollection<StableAlternative> splitAlternatives
);
```

Why `long?`? Because a stableId (canonical or child) might temporarily have no current entity (e.g. during cleanup or racey ingest).

---

## 2) Extend `IStableIdService` with a single “rich resolve” method

Update `IStableIdService.cs` (keep the existing methods, add one):

```csharp
using EntityResolutionSource.Senzing.Domain;

namespace EntityResolutionSource.Senzing.Interfaces;

public interface IStableIdService
{
    Task<EntityId> ResolveCanonicalStableIdAsync(EntityId stableId);

    Task<EntityId> UpsertStableIdForEntityAsync(
        long entityId,
        IReadOnlyCollection<RecordId> records
    );

    Task<(EntityId canonicalStableId, IReadOnlyCollection<long> entityIds)> ResolveAsync(EntityId externalStableId);

    /// <summary>
    /// Rich resolve for external callers:
    /// - canonical stable
    /// - single current entityId (1:1)
    /// - split alternatives (stableId + their current entityId)
    /// </summary>
    Task<StableResolution> ResolveWithAlternativesAsync(EntityId externalStableId);
}
```

This builds on your existing contract but gives you the API shape you described.

---

## 3) Minimal repository additions needed to power it

Your current repo can only store multiple entityIds per stable . For correct split behavior + “one current entity per stable”, add these to `IStableIdRepository`:

```csharp
// 1:1 "current pointer"
Task<long?> GetCurrentEntityIdForStableAsync(EntityId canonicalStableId);
Task SetCurrentEntityIdForStableAsync(EntityId canonicalStableId, long entityId);

// Optional reverse pointer (handy for “entity already has a stable”)
Task<EntityId?> GetCurrentStableIdForEntityAsync(long entityId);
Task SetCurrentStableIdForEntityAsync(long entityId, EntityId canonicalStableId);

// Split lineage
Task AddSplitChildAsync(EntityId parentCanonicalStableId, EntityId childCanonicalStableId);
Task<IReadOnlyCollection<EntityId>> GetSplitChildrenAsync(EntityId parentCanonicalStableId);
```

You can keep the existing `GetEntityIdsForStableAsync/SetEntityIdsForStableAsync` for backward compatibility , but your **new code should prefer** the “current pointer” methods.

---

## 4) Implement `ResolveWithAlternativesAsync` in `StableIdService`

Add this method to your updated `StableIdService`:

```csharp
using EntityResolutionSource.Senzing.Domain;
using EntityResolutionSource.Senzing.Interfaces;

namespace EntityResolutionSource.Senzing.Impl;

public sealed partial class StableIdService : IStableIdService
{
    public async Task<StableResolution> ResolveWithAlternativesAsync(EntityId externalStableId)
    {
        if (externalStableId is null) throw new ArgumentNullException(nameof(externalStableId));

        var canonical = await ResolveCanonicalStableIdAsync(externalStableId);

        // Canonical -> current entity (single)
        long? currentEntityId = await _stableIdRepository.GetCurrentEntityIdForStableAsync(canonical);

        // Canonical -> split children -> (child stable, child current entity)
        var childStableIds = await _stableIdRepository.GetSplitChildrenAsync(canonical);

        var alternatives = new List<StableAlternative>(childStableIds.Count);
        foreach (var child in childStableIds)
        {
            var childCanonical = await ResolveCanonicalStableIdAsync(child); // safe even if already canonical
            var childEntityId = await _stableIdRepository.GetCurrentEntityIdForStableAsync(childCanonical);

            alternatives.Add(new StableAlternative(childCanonical, childEntityId));
        }

        return new StableResolution(
            requestedStableId: externalStableId,
            canonicalStableId: canonical,
            currentEntityId: currentEntityId,
            splitAlternatives: alternatives
        );
    }
}
```

### Optional: keep `ResolveAsync` implemented via the new method

This guarantees that `ResolveAsync` never returns “multiple entityIds” anymore (unless you _want_ it to):

```csharp
public async Task<(EntityId canonicalStableId, IReadOnlyCollection<long> entityIds)> ResolveAsync(EntityId externalStableId)
{
    var res = await ResolveWithAlternativesAsync(externalStableId);

    return (
        res.canonicalStableId,
        res.currentEntityId is null ? Array.Empty<long>() : new[] { res.currentEntityId.Value }
    );
}
```

---

## 5) Example API endpoint (single round trip)

```csharp
using EntityResolutionSource.Senzing.Domain;
using EntityResolutionSource.Senzing.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/stable")]
public sealed class StableController : ControllerBase
{
    private readonly IStableIdService _stableIdService;

    public StableController(IStableIdService stableIdService)
    {
        _stableIdService = stableIdService;
    }

    [HttpGet("{stableId}")]
    public Task<StableResolution> GetStable(string stableId)
        => _stableIdService.ResolveWithAlternativesAsync(new EntityId(stableId));
}
```

This returns exactly what you described: if a client asks for a stale stableId (e.g. `SI001`) and it has split into other clusters, they’ll get:

- `canonicalStableId = SI001`
- `currentEntityId = <the one SI001 still points at>`
- `splitAlternatives = [(SI002, entityId2), (SI003, entityId3), ...]`

…and the client makes the choice.

---

If you want, I can also show how to **populate split lineage** deterministically during `UpsertStableIdForEntityAsync` (so the “parent” is always the stable that previously owned the record(s), not just “whatever candidate we saw first”).
