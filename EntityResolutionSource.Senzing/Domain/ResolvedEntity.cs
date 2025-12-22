namespace EntityResolutionSource.Senzing.Domain;

public class ResolvedEntity
{
    public ResolvedEntity(long entityId)
        : this(entityId, string.Empty, 0, new List<RecordSummary>()) { }

    public ResolvedEntity(
        long entityId,
        string entityName,
        int recordCount,
        IReadOnlyCollection<RecordSummary> records,
        bool isDead = false
    )
    {
        EntityId = entityId;
        EntityName = entityName;
        RecordCount = recordCount;
        Records = records;
        IsDead = isDead;
    }

    public long EntityId { get; set; }

    public string EntityName { get; set; }

    public int RecordCount { get; set; }

    public bool IsDead { get; set; }

    public IReadOnlyCollection<RecordSummary> Records { get; set; }
}
