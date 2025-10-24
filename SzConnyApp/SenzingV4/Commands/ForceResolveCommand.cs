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
        // Stage 0: Purge senzing repository `./SzConnyApp purge`
        
        
        // Stage 1: Seed data
        var sampleRecords = GetSampleRecords();
        
        foreach (var nestedRecord in sampleRecords)
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

        
        // Stage 2: Run 'SzConnyApp export' or sz_explorer - there should be 3 discrete entities

        
        // Stage 3: Force resolve record 101 and 103
        // foreach (var record in new[] { sampleRecords[0], sampleRecords[2] })
        // {
        //     var trustedIdFeature = new SenzingEntitySpecification
        //     {
        //         TrustedIdNumber = "TEST_101-TEST_103",
        //         TrustedIdType = SzTrustedIdType.ForceResolve
        //     };
        //     record.Features.Add(trustedIdFeature);
        //     var addRecordResponse = szEnvironment.AddNestedRecord(record);
        //     logger.LogInformation(
        //         $"Record {addRecordResponse?.RecordId} added with TRUSTED_ID, affected entities: {addRecordResponse?.AffectedEntities.Count}"
        //     );
        // }


        // Stage 4: Run 'SzConnyApp export' or sz_explorer `why 1` - there should be 2 discrete entities

    }

    private static NestedRecord[] GetSampleRecords()
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
