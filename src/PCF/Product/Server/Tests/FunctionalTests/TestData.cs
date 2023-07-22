namespace PCF.FunctionalTests
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    public static class TestData
    {
        // These values correspond to values in the INT configuration that are hardcoded. Test AgentId 2
        public const string TestAgentId = "9C14B08F-F064-48BA-A221-BE13F015CCBA";

        // Supports all 5 data types with custom predicates, all subject types, and all command types.
        public const string UniversalAssetGroupId = "18BDF572-9B49-4C6B-B5A0-3B5681D80AD8";

        // Supports all 5 data types with custom predicates, all subject types, and all command types.
        public const string UniversalAssetGroupQualifier = "AssetType=AzureTable;AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e";

        // Hardcoded msa subject taken from the PCF sample code.
        public static readonly MsaSubject SampleMsaSubject = new MsaSubject
        {
            Puid = 0x000300000A80842A,
            Xuid = "2535439314266772",
            Anid = "928D5FE23E2DD1F3F5AEAD19FFFFFFFF",
            Cid = 3219959886081161043,
            Opid = "vdPQVboQwcJSkQF2RgRnxA2"
        };
    }
}
