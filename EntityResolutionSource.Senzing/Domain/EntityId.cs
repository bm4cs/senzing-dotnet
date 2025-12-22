namespace EntityResolutionSource.Senzing.Domain;

public record EntityId(string entityId)
{
    public static EntityId New =>  new($"E-{Guid.NewGuid().ToString()}");

    public static EntityId Empty =>  new(string.Empty);
}