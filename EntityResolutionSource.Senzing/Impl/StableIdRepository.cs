using EntityResolutionSource.Senzing.Domain;
using EntityResolutionSource.Senzing.Interfaces;

namespace EntityResolutionSource.Senzing.Impl;

public class StableIdRepository : IStableIdRepository
{
    public Task CreateStableAsync(EntityId stableId)
    {
        throw new NotImplementedException();
    }

    public Task<EntityId?> GetCanonicalIdAsync(EntityId stableId)
    {
        throw new NotImplementedException();
    }

    public Task SetCanonicalIdAsync(EntityId stableId, EntityId canonicalId)
    {
        throw new NotImplementedException();
    }

    public Task<EntityId?> GetStableIdForRecordAsync(RecordId record)
    {
        throw new NotImplementedException();
    }

    public Task SetStableIdForRecordAsync(RecordId recordId, EntityId stableId)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<long>> GetEntityIdsForStableAsync(EntityId canonicalStableId)
    {
        throw new NotImplementedException();
    }

    public Task SetEntityIdsForStableAsync(EntityId canonicalStableId, IReadOnlyCollection<long> entityIds)
    {
        throw new NotImplementedException();
    }

    // public async Task CreateStableAsync(EntityId stableId)
    // {
    //     var command = new CreateEntityCommandV1(stableId) { Status = EntityStatusType.Birth };
    //     await commandBus.PublishAsync(command, CancellationToken.None);
    // }
    //
    // public async Task<EntityId?> GetCanonicalIdAsync(EntityId stableId)
    // {
    //     var entities = await entityRepository.GetByIdAsync(stableId,  CancellationToken.None);
    //     var canonicalId = entities.FirstOrDefault()?.CanonicalId;
    //     return canonicalId == null ? null : new EntityId(canonicalId);
    // }
    //
    // public async Task SetCanonicalIdAsync(EntityId stableId, EntityId canonicalId)
    // {
    //     var command = new SetStableEntityCanonicalIdCommandV1(stableId) { CanonicalId = canonicalId };
    //     await commandBus.PublishAsync(command, CancellationToken.None);
    // }
    //
    // public async Task<EntityId?> GetStableIdForRecordAsync(RecordId record)
    // {
    //     var entities = await entityRepository.GetByRecordIdAsync(record,  CancellationToken.None);
    //
    //     var entity = entities.FirstOrDefault();
    //
    //     if (entity == null)
    //         return null;
    //
    //     return entity.CanonicalId == null ? new EntityId(entity.EntityId) : new EntityId(entity.CanonicalId);
    // }
    //
    // public async Task SetStableIdForRecordAsync(RecordId recordId, EntityId stableId)
    // {
    //     // Purge any pre-existing knowledge of this RecordId not related to the target StableId
    //     var entities = await entityRepository.GetByRecordIdAsync(recordId,  CancellationToken.None);
    //     var stableIdAlreadyOwnsRecord = false;
    //
    //     foreach (var entity in entities)
    //     {
    //         var entityId = new EntityId(entity.EntityId);
    //         if (entityId == stableId)
    //         {
    //             stableIdAlreadyOwnsRecord = true;
    //             continue;
    //         }
    //
    //         var recordIds = entity.GetEntityState().RecordIds;
    //         var filteredRecordIds = recordIds.Where(r => !r.Equals(recordId.Value));
    //         var setRecordsCommand = new SetStableEntityRecordsCommandV1(entityId) { RecordIds = filteredRecordIds.Select(r => new RecordId(r)) };
    //         await commandBus.PublishAsync(setRecordsCommand, CancellationToken.None);
    //     }
    //
    //     if (stableIdAlreadyOwnsRecord)
    //     {
    //         return;
    //     }
    //
    //     // Add RecordId the stable entity that now owns it, but doesn't have pre-existing knowledge of it
    //     var stableEntity = (await entityRepository.GetByIdAsync(stableId,  CancellationToken.None)).First();
    //     var stableEntityId = new EntityId(stableEntity.EntityId);
    //     var stableRecordIds = stableEntity.GetEntityState().RecordIds;
    //     var stableRecordIdsPlusNewRecord = stableRecordIds.Add(recordId.Value);
    //     var command = new SetStableEntityRecordsCommandV1(stableEntityId) { RecordIds = stableRecordIdsPlusNewRecord.Select(r => new RecordId(r)) };
    //     await commandBus.PublishAsync(command, CancellationToken.None);
    // }
    //
    // public async Task<Seq<long>> GetEntityIdsForStableAsync(EntityId canonicalStableId)
    // {
    //     var entities = await entityRepository.GetByCanonicalIdAsync(canonicalStableId,  CancellationToken.None);
    //     var entity = entities.FirstOrDefault();
    //     return entity?.GetEntityState().InternalEntityIds ?? [];
    // }
    //
    // public async Task SetEntityIdsForStableAsync(EntityId canonicalStableId,
    //     Seq<long> entityIds)
    // {
    //     var entities = await entityRepository.GetByCanonicalIdAsync(canonicalStableId,  CancellationToken.None);
    //     var stableEntityId = new EntityId(entities.First().EntityId);
    //
    //     var command = new SetStableEntityInternalEntityIdsCommandV1(stableEntityId) { InternalEntityIds = entityIds };
    //     await commandBus.PublishAsync(command, CancellationToken.None);
    // }
}
