using EntityResolutionSource.Senzing.Interfaces;
using EntityResolutionSource.Senzing.Domain;
using EntityResolutionSource.Senzing.Interfaces;

namespace EntityResolutionSource.Senzing.Impl;

public class EntityStateRepository
    : IEntityStateRepository
{
    public Task<bool> WasKnownToExist(long entityId)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<RecordId>> GetKnownRecords(long entityId)
    {
        throw new NotImplementedException();
    }

    public Task SaveEntitySnapshot(long entityId, bool exists, IReadOnlyCollection<RecordId> records)
    {
        throw new NotImplementedException();
    }
}
