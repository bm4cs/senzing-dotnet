namespace EntityResolutionSource.Senzing.Domain;

public record RecordSummary(
    string DataSource,
    string ErruleCode,
    DateTimeOffset FirstSeenDt,
    int InternalId,
    DateTimeOffset LastSeenDt,
    string MatchKey,
    string MatchLevelCode,
    string RecordId
);
