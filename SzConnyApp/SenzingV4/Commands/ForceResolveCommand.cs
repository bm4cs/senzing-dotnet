using Microsoft.Extensions.Logging;
using Senzing.Typedef;
using SzConnyApp.SenzingV4.Models;
using SzConnyApp.SenzingV4.Senzing;
using static Senzing.Sdk.SzFlags;

namespace SzConnyApp.SenzingV4.Commands;

public class ForceResolveCommand(
    ISzEnvironmentWrapper szEnvironment,
    ILogger<RecordLoaderCommand> logger
) : IForceResolveCommand
{
    public void Execute()
    {
        // Stage 1: Seed data
        foreach (var nestedRecord in GetNestedRecords())
        {
            var addRecordResponse = szEnvironment.AddNestedRecord(nestedRecord);

            logger.LogInformation(
                $"Record {addRecordResponse?.RecordId} added, affected entities: {addRecordResponse?.AffectedEntities.Count}"
            );

            if ((bool)addRecordResponse?.AffectedEntities.Any())
            {
                foreach (var affectedEntity in addRecordResponse?.AffectedEntities)
                {
                    logger.LogInformation($"Affected Entity ID: {affectedEntity?.EntityId}");
                }
            }
        }

        // Stage 2: Run 'SzConnyApp export' or sz_explorer - the 3 record should have created 3 separate entities

        // Stage 3: Force resolve record 101 and 103
        var record101 = szEnvironment.GetRecord("TEST", "101");
        var record103 = szEnvironment.GetRecord("TEST", "103");
        
        // Stage 4: 
    }

    private static NestedRecord[] GetNestedRecords()
    {
        return
        [
            new NestedRecord(
                "TEST",
                "101",
                new List<SenzingEntitySpecification>()
                {
                    new() { RecordType = "PERSON" },
                    new() { NameType = SzNameTypes.Primary, NameFull = "Patrick Smith" },
                    new() { NameType = SzNameTypes.Aka, NameFull = "Paddy Smith" },
                    new()
                    {
                        AddrType = SzAddressTypes.Home,
                        AddrFull = "7-9 West St, North Sydney, NSW, 2060",
                    },
                    new() { PhoneType = SzPhoneTypes.Home, PhoneNumber = "(02) 9956 5230" },
                    new() { DateOfBirth = "1990-12-01" },
                }
            ),
            new NestedRecord(
                "TEST",
                "102",
                new List<SenzingEntitySpecification>()
                {
                    new() { RecordType = "PERSON" },
                    new() { NameType = SzNameTypes.Primary, NameFull = "Patricia Smith" },
                    new() { NameType = SzNameTypes.Aka, NameFull = "Paddy Smith" },
                    new()
                    {
                        AddrType = SzAddressTypes.Home,
                        AddrFull = "7-9 West St, North Sydney, NSW, 2060",
                    },
                    new() { PhoneType = SzPhoneTypes.Home, PhoneNumber = "(02) 9956 5230" },
                    new() { DateOfBirth = "1994-05-04" },
                }
            ),
            new NestedRecord(
                "TEST",
                "103",
                new List<SenzingEntitySpecification>()
                {
                    new() { RecordType = "PERSON" },
                    new() { NameType = SzNameTypes.Primary, NameFull = "Pat Smith" },
                    new()
                    {
                        AddrType = SzAddressTypes.Home,
                        AddrFull = "7-9 West St, North Sydney, NSW, 2060",
                    },
                    new() { PhoneType = SzPhoneTypes.Home, PhoneNumber = "(02) 9956 5230" },
                }
            ),
        ];
    }
}
