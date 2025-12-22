namespace EntityResolutionSource.Senzing.Core;

public static class SzConstants
{
    public static class DataSources
    {
        public const string Ems = "EMS";
    }

    public static class SzAddressTypes
    {
        public const string Business = "BUSINESS";
        public const string Home = "HOME";
        public const string Mailing = "MAILING";
    }

    public class SzNameTypes
    {
        public const string Primary = "PRIMARY";
        public const string Aka = "AKA";
        public const string Dba = "DBA";
        public const string NickName = "NICKNAME";
    }

    public static class SzPhoneTypes
    {
        public const string Fax = "FAX";
        public const string Home = "HOME";
        public const string Mobile = "MOBILE";
        public const string Work = "WORK";
    }

    public static class SzRecordTypes
    {
        public const string Person = "PERSON";
        public const string Organisation = "ORGANIZATION";
    }

    public static class SzTrustedIdTypes
    {
        public const string ForceResolve = "FORCE_RESOLVE";
    }
}
