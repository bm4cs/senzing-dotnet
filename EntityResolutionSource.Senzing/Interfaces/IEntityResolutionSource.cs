using EntityResolutionSource.Senzing.Models;
using EntityResolutionSource.Senzing.Domain;

namespace EntityResolutionSource.Senzing.Interfaces;

public interface IEntityResolutionSource
{
    Task<EntityResolutionResult> ResolveAsync(
        RecordId recordId,
        RecordV4 record
    );
}