using System.Text.Json;
using Microsoft.Extensions.Logging;
using Senzing.Sdk;
using Senzing.Typedef;
using SzConnyApp.SenzingV4.Models;
using SzConnyApp.SenzingV4.Senzing;
using static Senzing.Sdk.SzFlags;

namespace SzConnyApp.SenzingV4.Commands;

public class RecordLoaderCommand(
    ISzEnvironmentWrapper szEnvironment,
    ILogger<RecordLoaderCommand> logger
) : IRecordLoaderCommand
{
    public void Execute()
    {
        foreach (var record in GetRecords())
        {
            SzEngineAddRecordResponse? addRecordResponse = null;

            switch (record)
            {
                case FlatRecord flatRecord:
                    addRecordResponse = szEnvironment.AddFlatRecord(flatRecord);
                    break;
                case NestedRecord nestedRecord:
                    addRecordResponse = szEnvironment.AddNestedRecord(nestedRecord);
                    break;
            }

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
    }

    private static AbstractRecord[] GetRecords()
    {
        return
        [
            new FlatRecord(
                "TEST",
                "1001",
                new SenzingEntitySpecification
                {
                    RecordType = "PERSON",
                    PrimaryNameFirst = "Robert",
                    PrimaryNameLast = "Smith",
                    DateOfBirth = "12/11/1978",
                    AddrType = "MAILING",
                    AddrFull = "123 Main Street, Las Vegas, NV 89132",
                    PhoneType = "HOME",
                    PhoneNumber = "702-919-1300",
                    EmailAddress = "bsmith@work.com",
                }
            ),
            new FlatRecord(
                "TEST",
                "1002",
                new SenzingEntitySpecification
                {
                    RecordType = "PERSON",
                    PrimaryNameFirst = "Bob",
                    PrimaryNameLast = "Smith",
                    DateOfBirth = "11/12/1978",
                    AddrType = "HOME",
                    AddrLine1 = "1515 Adela Lane",
                    AddrCity = "Las Vegas",
                    AddrState = "NV",
                    AddrPostalCode = "89111",
                    PhoneType = "MOBILE",
                    PhoneNumber = "702-919-1300",
                }
            ),
            new FlatRecord(
                "TEST",
                "1003",
                new SenzingEntitySpecification
                {
                    RecordType = "PERSON",
                    PrimaryNameFirst = "Bob",
                    PrimaryNameLast = "Smith",
                    PrimaryNameMiddle = "J",
                    DateOfBirth = "12/11/1978",
                    EmailAddress = "bsmith@work.com",
                }
            ),
            new FlatRecord(
                "TEST",
                "1004",
                new SenzingEntitySpecification
                {
                    RecordType = "PERSON",
                    PrimaryNameFirst = "B",
                    PrimaryNameLast = "Smith",
                    AddrType = "HOME",
                    AddrLine1 = "1515 Adela Ln",
                    AddrCity = "Las Vegas",
                    AddrState = "NV",
                    AddrPostalCode = "89132",
                    EmailAddress = "bsmith@work.com",
                }
            ),
            new FlatRecord(
                "TEST",
                "1005",
                new SenzingEntitySpecification
                {
                    RecordType = "PERSON",
                    PrimaryNameFirst = "Rob",
                    PrimaryNameMiddle = "E",
                    PrimaryNameLast = "Smith",
                    DriversLicenseNumber = "112233",
                    DriversLicenseState = "NV",
                    AddrType = "MAILING",
                    AddrLine1 = "123 E Main St",
                    AddrCity = "Henderson",
                    AddrState = "NV",
                    AddrPostalCode = "89132",
                }
            ),
            new FlatRecord(
                "TEST",
                "1006",
                new SenzingEntitySpecification
                {
                    RecordType = "PERSON",
                    PrimaryNameFirst = "John",
                    PrimaryNameLast = "Carmack",
                    DriversLicenseNumber = "11333377",
                    DriversLicenseState = "NSW",
                    AddrType = "HOME",
                    AddrLine1 = "100 Mutex Street",
                    AddrCity = "Rosebery",
                    AddrState = "NSW",
                    AddrPostalCode = "2018",
                }
            ),
            new FlatRecord(
                "TEST",
                "1007",
                new SenzingEntitySpecification
                {
                    RecordType = "PERSON",
                    PrimaryNameFirst = "J",
                    PrimaryNameLast = "Carmack",
                }
            ),
        ];
    }

    private static NestedRecord[] GetNestedRecords()
    {
        return
        [
            new NestedRecord(
                "TEST",
                "1008",
                new List<SenzingEntitySpecification>()
                {
                    new() { RecordType = "PERSON" },
                    new()
                    {
                        NameType = SzNameTypes.Primary,
                        NameFirst = "Johny",
                        NameLast = "Mack",
                    },
                    new()
                    {
                        AddrType = SzAddressTypes.Home,
                        AddrLine1 = "100 Mutex Street",
                        AddrCity = "Rosebery",
                        AddrState = "NSW",
                        AddrPostalCode = "2018",
                    },
                }
            ),
            new NestedRecord(
                "TEST",
                "1009",
                new List<SenzingEntitySpecification>()
                {
                    new() { RecordType = "PERSON" },
                    new()
                    {
                        NameType = SzNameTypes.Primary,
                        PrimaryNameFirst = "J",
                        PrimaryNameLast = "Carmack",
                    },
                    new() { NameType = SzNameTypes.NickName, NameLast = "J Mack" },
                    new()
                    {
                        AddrType = SzAddressTypes.Home,
                        AddrLine1 = "100 Mutex Street",
                        AddrCity = "Rosebery",
                        AddrState = "NSW",
                        AddrPostalCode = "2018",
                    },
                }
            ),
        ];
    }
}
