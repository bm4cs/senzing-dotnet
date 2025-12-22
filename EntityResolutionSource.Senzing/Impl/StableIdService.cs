using System.Collections.Immutable;
using EntityResolutionSource.Senzing.Domain;
using EntityResolutionSource.Senzing.Interfaces;

namespace EntityResolutionSource.Senzing.Impl;

/// <summary>
/// Stable external ID layer on top of Senzing.
/// All aliasing (pointer redirection) is persisted using IStableIdRepository.
/// </summary>
public sealed class StableIdService : IStableIdService
{
    private readonly IStableIdRepository _stableIdRepository;

    public StableIdService(IStableIdRepository stableIdRepository)
    {
        _stableIdRepository = stableIdRepository;
    }

    /// <summary>
    /// Resolve the canonical StableId given any (possibly old/alias) StableId.
    /// This just chases CanonicalId pointers in the DB until we reach a root.
    /// </summary>
    public async Task<EntityId> ResolveCanonicalStableIdAsync(EntityId stableId)
    {
        var seen = new HashSet<EntityId>();

        while (true)
        {
            if (!seen.Add(stableId))
            {
                throw new InvalidOperationException(
                    $"Cycle detected in StableId aliases at {stableId}"
                );
            }

            var canonical = await _stableIdRepository.GetCanonicalIdAsync(stableId);

            // If null or canonical == stableId => treat as root
            if (canonical == null || canonical == stableId)
                return stableId;

            stableId = canonical;
        }
    }

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
    public async Task<EntityId> UpsertStableIdForEntityAsync(long entityId, IReadOnlyCollection<RecordId> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        // 1. Discover which stable IDs these records are already tied to.
        var existingCanonicalStableIds = new HashSet<EntityId>();

        foreach (var record in records)
        {
            var stableId = await _stableIdRepository.GetStableIdForRecordAsync(record);
            if (stableId != null)
            {
                var canonical = await ResolveCanonicalStableIdAsync(stableId);

                //TODO: dont blend with an existing stable
                // 1. Pull all the existing records associated with the canonical stable ID
                // 2. If the existing stable entity cluster contains any records that is not one of the records associated with the affected entity
                // 3.
                // var canonicalEntity = _stableIdRepository

                existingCanonicalStableIds.Add(canonical);
            }
        }

        EntityId survivor;

        if (existingCanonicalStableIds.Count == 0)
        {
            // New logical entity.
            survivor = EntityId.New;
            await _stableIdRepository.CreateStableAsync(survivor);
        }
        else if (existingCanonicalStableIds.Count == 1)
        {
            // All records agree on a single canonical StableId.
            survivor = existingCanonicalStableIds.First();
        }
        else
        {
            // Multiple canonical StableIds now appear in the same Senzing entity.
            // => We've just discovered a "merge" at the logical level:
            //    all of those stable IDs actually refer to the same real-world entity.
            // Pick a survivor and redirect others to it.
            survivor = PickSurvivor(existingCanonicalStableIds);

            foreach (var loser in existingCanonicalStableIds)
            {
                if (loser == survivor)
                    continue;

                // Set loser.CanonicalId = survivor in DB.
                await _stableIdRepository.SetCanonicalIdAsync(loser, survivor);
            }
        }

        // 2. Ensure all records for this entity point at the survivor StableId.
        foreach (var record in records)
        {
            await _stableIdRepository.SetStableIdForRecordAsync(record, survivor);
        }

        // 3. Rebuild StableId -> EntityIds mapping for the survivor.
        var entitiesForSurvivor = new HashSet<long>(
            await _stableIdRepository.GetEntityIdsForStableAsync(survivor)
        )
        {
            entityId
        };


        await _stableIdRepository.SetEntityIdsForStableAsync(survivor, new ImmutableList<long>.Creeate(entitiesForSurvivor));

        return survivor;
    }

    /// <summary>
    /// Choose surviving StableId when merging.
    /// Several options:
    ///  - smallest GUID
    ///  - oldest record
    ///  - other custom rules
    /// </summary>
    private static EntityId PickSurvivor(HashSet<EntityId> candidates)
    {
        // lowest GUID, deterministic and stable
        var survivor = EntityId.Empty;
        foreach (var id in candidates)
        {
            if (survivor == EntityId.Empty || id == mpareTo(survivor) < 0)
                survivor = id;
        }
        return survivor;
    }

    /// <summary>
    /// Surface canonical ID to outside world.
    /// given a stable ID from a client (which may be old),
    /// return the current canonical StableId and the Senzing entity IDs that represent it.
    /// </summary>
    public async Task<(EntityId canonicalStableId, Seq<long> entityIds)> ResolveAsync(
        EntityId externalStableId
    )
    {
        var canonical = await ResolveCanonicalStableIdAsync(externalStableId);
        var entityIds = await _stableIdRepository.GetEntityIdsForStableAsync(canonical);
        return (canonical, entityIds);
    }
}
