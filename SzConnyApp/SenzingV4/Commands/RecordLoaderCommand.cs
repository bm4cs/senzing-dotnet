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
        var szEngine = szEnvironment.Engine;
        foreach (var record in GetRecords())
        {
            var jsonString = szEngine.AddRecord(
                record.DataSourceCode,
                record.RecordId,
                record.EntityJson,
                SzFlag.SzWithInfo
            );
            var addRecordResponse = JsonSerializer.Deserialize<SzEngineAddRecordResponse>(
                jsonString
            );
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

    private static EntityRecord[] GetRecords()
    {
        return
        [
            new EntityRecord(
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
            new EntityRecord(
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
            new EntityRecord(
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
            new EntityRecord(
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
            new EntityRecord(
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
            new EntityRecord(
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
            new EntityRecord(
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

    // private static IList<DumbRecord> GetRecords()
    // {
    //     return
    //     [
    //         new DumbRecord(
    //             "TEST",
    //             "1001",
    //             """
    //             {
    //                 "DATA_SOURCE": "TEST",
    //                 "RECORD_ID": "1001",
    //                 "RECORD_TYPE": "PERSON",
    //                 "PRIMARY_NAME_FIRST": "Robert",
    //                 "PRIMARY_NAME_LAST": "Smith",
    //                 "DATE_OF_BIRTH": "12/11/1978",
    //                 "ADDR_TYPE": "MAILING",
    //                 "ADDR_FULL": "123 Main Street, Las Vegas, NV 89132",
    //                 "PHONE_TYPE": "HOME",
    //                 "PHONE_NUMBER": "702-919-1300",
    //                 "EMAIL_ADDRESS": "bsmith@work.com"
    //             }
    //             """
    //         ),
    //         new DumbRecord(
    //             "TEST",
    //             "1002",
    //             """
    //             {
    //                 "DATA_SOURCE": "TEST",
    //                 "RECORD_ID": "1002",
    //                 "RECORD_TYPE": "PERSON",
    //                 "PRIMARY_NAME_FIRST": "Bob",
    //                 "PRIMARY_NAME_LAST": "Smith",
    //                 "PRIMARY_NAME_GENERATION": "II",
    //                 "DATE_OF_BIRTH": "11/12/1978",
    //                 "ADDR_TYPE": "HOME",
    //                 "ADDR_LINE1": "1515 Adela Lane",
    //                 "ADDR_CITY": "Las Vegas",
    //                 "ADDR_STATE": "NV",
    //                 "ADDR_POSTAL_CODE": "89111",
    //                 "PHONE_TYPE": "MOBILE",
    //                 "PHONE_NUMBER": "702-919-1300"
    //             }
    //             """
    //         ),
    //         new DumbRecord(
    //             "TEST",
    //             "1003",
    //             """
    //             {
    //                 "DATA_SOURCE": "TEST",
    //                 "RECORD_ID": "1003",
    //                 "RECORD_TYPE": "PERSON",
    //                 "PRIMARY_NAME_FIRST": "Bob",
    //                 "PRIMARY_NAME_LAST": "Smith",
    //                 "PRIMARY_NAME_MIDDLE": "J",
    //                 "DATE_OF_BIRTH": "12/11/1978",
    //                 "EMAIL_ADDRESS": "bsmith@work.com"
    //             }
    //             """
    //         ),
    //         new DumbRecord(
    //             "TEST",
    //             "1004",
    //             """
    //             {
    //                 "DATA_SOURCE": "TEST",
    //                 "RECORD_ID": "1004",
    //                 "RECORD_TYPE": "PERSON",
    //                 "PRIMARY_NAME_FIRST": "B",
    //                 "PRIMARY_NAME_LAST": "Smith",
    //                 "ADDR_TYPE": "HOME",
    //                 "ADDR_LINE1": "1515 Adela Ln",
    //                 "ADDR_CITY": "Las Vegas",
    //                 "ADDR_STATE": "NV",
    //                 "ADDR_POSTAL_CODE": "89132",
    //                 "EMAIL_ADDRESS": "bsmith@work.com"
    //             }
    //             """
    //         ),
    //         new DumbRecord(
    //             "TEST",
    //             "1005",
    //             """
    //             {
    //                 "DATA_SOURCE": "TEST",
    //                 "RECORD_ID": "1005",
    //                 "RECORD_TYPE": "PERSON",
    //                 "PRIMARY_NAME_FIRST": "Rob",
    //                 "PRIMARY_NAME_MIDDLE": "E",
    //                 "PRIMARY_NAME_LAST": "Smith",
    //                 "DRIVERS_LICENSE_NUMBER": "112233",
    //                 "DRIVERS_LICENSE_STATE": "NV",
    //                 "ADDR_TYPE": "MAILING",
    //                 "ADDR_LINE1": "123 E Main St",
    //                 "ADDR_CITY": "Henderson",
    //                 "ADDR_STATE": "NV",
    //                 "ADDR_POSTAL_CODE": "89132"
    //             }
    //             """
    //         ),
    //     ];
    // }
}
