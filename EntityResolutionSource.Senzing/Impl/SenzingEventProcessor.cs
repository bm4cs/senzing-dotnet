using EntityResolutionSource.Senzing.Interfaces;
using EntityResolutionSource.Senzing.Domain;
using EntityResolutionSource.Senzing.Interfaces;
using Senzing.Sdk;

namespace EntityResolutionSource.Senzing.Impl;

/// <summary>
/// Wraps SzEngine and applies the "robust per-event processing pattern"
/// for AddRecord/DeleteRecord/ProcessRedoRecord WithInfo payloads.
/// </summary>
public sealed class SenzingEventProcessor : IEntityResolutionEngineEventProcessor
{
    private readonly ISzEnvironmentWrapper _szEnvironment;
    private readonly IEntityStateRepository _stateRepo;

    public SenzingEventProcessor(
        ISzEnvironmentWrapper szEnvironment,
        IEntityStateRepository stateRepo
    )
    {
        _szEnvironment = szEnvironment;
        _stateRepo = stateRepo;
    }

    public async Task<IReadOnlyList<EntityChangeSummary>> ProcessAffectedEntities(IReadOnlyCollection<long> affectedEntityIds)
    {
        if (affectedEntityIds.Count == 0)
        {
            return [];
        }

        // 2. Pre-state from *our* DB: preExists, preRecords.
        var preExists = new Dictionary<long, bool>();
        var preRecords = new Dictionary<long, List<RecordId>>();

        foreach (long entityId in affectedEntityIds)
        {
            bool existed = await _stateRepo.WasKnownToExist(entityId);
            preExists[entityId] = existed;

            var records = (await _stateRepo.GetKnownRecords(entityId)).ToList() ?? [];

            preRecords[entityId] = records;
        }

        // 3. Post-state from Senzing GetEntity: postExists, postRecords.
        var postExists = new Dictionary<long, bool>();
        var postRecords = new Dictionary<long, List<RecordId>>();

        foreach (long entityId in affectedEntityIds)
        {
            try
            {
                var entity = await _szEnvironment.GetEntityV4Async(entityId);
                var recordIds = entity
                    .ResolvedEntity.Records.Select(r => new RecordId(r.RecordId))
                    .ToList();

                postExists[entityId] = true;
                postRecords[entityId] = recordIds;
            }
            catch (SzNotFoundException)
            {
                // Entity ID is no longer valid
                postExists[entityId] = false;
                postRecords[entityId] = [];
            }
        }

        // 4. Build currentByRecord: RecordId -> Set of entity IDs that own it now
        var currentByRecord = new Dictionary<RecordId, HashSet<long>>();

        foreach (var (entityId, recordIds) in postRecords)
        {
            foreach (var recordId in recordIds)
            {
                if (!currentByRecord.TryGetValue(recordId, out var owningEntityIds))
                {
                    owningEntityIds = [];
                    currentByRecord[recordId] = owningEntityIds;
                }

                owningEntityIds.Add(entityId);
            }
        }

        // 5. For entities that *used* to exist, figure out where their records went
        var deletedRecords = new Dictionary<long, List<RecordId>>();
        var nextEntities = new Dictionary<long, HashSet<long>>();

        foreach (long entityId in affectedEntityIds)
        {
            bool existedBefore = preExists.TryGetValue(entityId, out var ex) && ex;
            if (!existedBefore)
            {
                continue; // Nothing to compare.
            }

            List<RecordId> oldRecordIds = preRecords[entityId];

            var nextSet = new HashSet<long>();
            var deletedList = new List<RecordId>();

            foreach (RecordId oldRecordId in oldRecordIds)
            {
                if (
                    currentByRecord.TryGetValue(oldRecordId, out var ownersNow)
                    && ownersNow.Count > 0
                )
                {
                    // A record usually belongs to one entity, but this is flexible
                    foreach (long owner in ownersNow)
                    {
                        nextSet.Add(owner);
                    }
                }
                else
                {
                    // We don't see this record in any entity now -> treat as deleted.
                    deletedList.Add(oldRecordId);
                }
            }

            if (nextSet.Count > 0)
            {
                nextEntities[entityId] = nextSet;
            }

            if (deletedList.Count > 0)
            {
                deletedRecords[entityId] = deletedList;
            }
        }

        // 6. Build contributors: for each entity that exists now, which old entity IDs fed into it
        var contributors = new Dictionary<long, HashSet<long>>();

        foreach (var (currentEntityId, oldContributingEntityIds) in nextEntities)
        {
            foreach (long oldContributingEntityId in oldContributingEntityIds)
            {
                if (!contributors.TryGetValue(oldContributingEntityId, out var set))
                {
                    set = new HashSet<long>();
                    contributors[oldContributingEntityId] = set;
                }

                set.Add(currentEntityId);
            }
        }

        // 7. Classify change kind for each affected entity ID.
        var results = new List<EntityChangeSummary>();

        foreach (long entityId in affectedEntityIds)
        {
            var didEntityExistBefore =
                preExists.TryGetValue(entityId, out var exBefore) && exBefore;
            var doesEntityExistNow = postExists.TryGetValue(entityId, out var exNow) && exNow;

            var beforeRecordsForEntity = preRecords.TryGetValue(entityId, out var pr) ? pr : [];

            var afterRecordsForEntity = postRecords.TryGetValue(entityId, out var po) ? po : [];

            var nextEntitiesForEntity = nextEntities.TryGetValue(entityId, out var ns) ? ns : [];

            var deletedRecordsForEntity = deletedRecords.TryGetValue(entityId, out var dr)
                ? dr
                : [];

            // Default classification
            var kind = EntityStatusType.Unknown;

            if (!didEntityExistBefore && doesEntityExistNow)
            {
                // Newly created entity ID
                kind = EntityStatusType.Birth;
            }
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
            }
            else if (didEntityExistBefore && doesEntityExistNow)
            {
                // Same entity ID, but may have gained or lost records
                var oldSet = new HashSet<RecordId>(
                    beforeRecordsForEntity
                );
                var newSet = new HashSet<RecordId>(
                    afterRecordsForEntity
                );

                bool lostAny = oldSet.Except(newSet).Any();
                bool gainedAny = newSet.Except(oldSet).Any();

                if (!lostAny && !gainedAny)
                {
                    kind = EntityStatusType.Unchanged;
                }
                else if (gainedAny && !lostAny)
                {
                    kind = EntityStatusType.Grow;
                }
                else if (!gainedAny && lostAny)
                {
                    kind = EntityStatusType.Shrink;
                }
                else
                {
                    // Both some records left, some came in
                    kind = EntityStatusType.Changed;
                }
            }

            // Contributors (from survivors perspective)
            var contributorSet = contributors.TryGetValue(entityId, out var cs) ? cs : [];

            var summary = new EntityChangeSummary
            {
                EntityId = entityId,
                PreExists = didEntityExistBefore,
                PostExists = doesEntityExistNow,
                PreRecords = beforeRecordsForEntity,
                PostRecords = afterRecordsForEntity,
                NextEntities = nextEntitiesForEntity,
                DeletedRecords = deletedRecordsForEntity,
                Status = kind,
                Contributors = contributorSet,
            };

            results.Add(summary);
        }

        // 8. Persist our new snapshot of entity -> records.
        foreach (long entityId in affectedEntityIds)
        {
            var existsNow = postExists.TryGetValue(entityId, out var exNow) && exNow;
            var nowRecords = postRecords.TryGetValue(entityId, out var list) ? list : [];
            await _stateRepo.SaveEntitySnapshot(entityId, existsNow, nowRecords);
        }

        return results;
    }
}
