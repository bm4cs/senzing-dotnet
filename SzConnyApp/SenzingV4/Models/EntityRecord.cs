using System.Text.Json;
using Senzing.Typedef;

namespace SzConnyApp.SenzingV4.Models;

public class EntityRecord(string dataSourceCode, string recordId, SenzingEntitySpecification entity)
{
    public string RecordId { get; private set; } = recordId;

    public string DataSourceCode { get; private set; } = dataSourceCode;

    public SenzingEntitySpecification Entity { get; private set; } = entity;

    public string EntityJson => JsonSerializer.Serialize(Entity);
}
