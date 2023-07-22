namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using Microsoft.PrivacyServices.Identity;

    using Newtonsoft.Json;

    using Xunit;

    public class AssetQualifierConverterTest
    {
        [Fact]
        public void Test()
        {
            var obj = new TestClass { Qualifier = AssetQualifier.CreateForAzureTable("accountName") };
            var json = JsonConvert.SerializeObject(obj);
            var obj2 = JsonConvert.DeserializeObject<TestClass>(json);

            Assert.Equal(obj.Qualifier, obj2.Qualifier);
        }

        public class TestClass
        {
            [JsonConverter(typeof(AssetQualifierConverter))]
            public AssetQualifier Qualifier { get; set; }
        }
    }
}