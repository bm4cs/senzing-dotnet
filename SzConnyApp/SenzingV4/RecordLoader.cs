using Microsoft.Extensions.Logging;
using Senzing.Sdk;
using SzConnyApp.SenzingV4.Models;
using SzConnyApp.SenzingV4.Senzing;
using static Senzing.Sdk.SzFlags;

namespace SzConnyApp.SenzingV4;

public class RecordLoader : IRecordLoader
{
    private readonly ILogger<RecordLoader> _logger;
    private readonly ISzEnvironmentWrapper _szEnvironment;
    
    public RecordLoader(ISzEnvironmentWrapper szEnvironment, ILogger<RecordLoader> logger)
    {
        _logger = logger;
        _szEnvironment = szEnvironment;
    }
    
    public void Execute()
    {
        var szEngine = _szEnvironment.Engine;
        foreach (var record in GetRecords())
        {
            // var (dataSourceCode, recordId) = keyTuple;
            szEngine.AddRecord(record.DataSourceCode, record.RecordId, record.JsonText, SzNoFlags);
            _logger.LogInformation($"Record {record.RecordId} added");
        }
    }
    
    private static IList<DumbRecord> GetRecords()
    {
        return
        [
            new DumbRecord(
                "TEST",
                "1001",
                """
                {
                    "DATA_SOURCE": "TEST",
                    "RECORD_ID": "1001",
                    "RECORD_TYPE": "PERSON",
                    "PRIMARY_NAME_FIRST": "Robert",
                    "PRIMARY_NAME_LAST": "Smith",
                    "DATE_OF_BIRTH": "12/11/1978",
                    "ADDR_TYPE": "MAILING",
                    "ADDR_FULL": "123 Main Street, Las Vegas, NV 89132",
                    "PHONE_TYPE": "HOME",
                    "PHONE_NUMBER": "702-919-1300",
                    "EMAIL_ADDRESS": "bsmith@work.com"
                }
                """
            ),
            new DumbRecord(
                "TEST",
                "1002",
                """
                {
                    "DATA_SOURCE": "TEST",
                    "RECORD_ID": "1002",
                    "RECORD_TYPE": "PERSON",
                    "PRIMARY_NAME_FIRST": "Bob",
                    "PRIMARY_NAME_LAST": "Smith",
                    "PRIMARY_NAME_GENERATION": "II",
                    "DATE_OF_BIRTH": "11/12/1978",
                    "ADDR_TYPE": "HOME",
                    "ADDR_LINE1": "1515 Adela Lane",
                    "ADDR_CITY": "Las Vegas",
                    "ADDR_STATE": "NV",
                    "ADDR_POSTAL_CODE": "89111",
                    "PHONE_TYPE": "MOBILE",
                    "PHONE_NUMBER": "702-919-1300"
                }
                """
            ),
            new DumbRecord(
                "TEST",
                "1003",
                """
                {
                    "DATA_SOURCE": "TEST",
                    "RECORD_ID": "1003",
                    "RECORD_TYPE": "PERSON",
                    "PRIMARY_NAME_FIRST": "Bob",
                    "PRIMARY_NAME_LAST": "Smith",
                    "PRIMARY_NAME_MIDDLE": "J",
                    "DATE_OF_BIRTH": "12/11/1978",
                    "EMAIL_ADDRESS": "bsmith@work.com"
                }
                """
            ),
            new DumbRecord(
                "TEST",
                "1004",
                """
                {
                    "DATA_SOURCE": "TEST",
                    "RECORD_ID": "1004",
                    "RECORD_TYPE": "PERSON",
                    "PRIMARY_NAME_FIRST": "B",
                    "PRIMARY_NAME_LAST": "Smith",
                    "ADDR_TYPE": "HOME",
                    "ADDR_LINE1": "1515 Adela Ln",
                    "ADDR_CITY": "Las Vegas",
                    "ADDR_STATE": "NV",
                    "ADDR_POSTAL_CODE": "89132",
                    "EMAIL_ADDRESS": "bsmith@work.com"
                }
                """
            ),
            new DumbRecord(
                "TEST",
                "1005",
                """
                {
                    "DATA_SOURCE": "TEST",
                    "RECORD_ID": "1005",
                    "RECORD_TYPE": "PERSON",
                    "PRIMARY_NAME_FIRST": "Rob",
                    "PRIMARY_NAME_MIDDLE": "E",
                    "PRIMARY_NAME_LAST": "Smith",
                    "DRIVERS_LICENSE_NUMBER": "112233",
                    "DRIVERS_LICENSE_STATE": "NV",
                    "ADDR_TYPE": "MAILING",
                    "ADDR_LINE1": "123 E Main St",
                    "ADDR_CITY": "Henderson",
                    "ADDR_STATE": "NV",
                    "ADDR_POSTAL_CODE": "89132"
                }
                """
            ),
        ];
    }

    
}
