namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;

    using Microsoft.PrivacyServices.DocumentDB.Models;

    using Newtonsoft.Json;

    using Xunit;

    public class DateTimeOffsetConverterTest
    {
        [Fact]
        public void TestNonNullDateTimeObject()
        {
            var obj = new TestClass { Expiry = DateTimeOffset.UtcNow };
            var json = JsonConvert.SerializeObject(obj);
            var obj2 = JsonConvert.DeserializeObject<TestClass>(json);

            Assert.Equal(obj.Expiry, obj2.Expiry);
        }

        [Fact]
        public void TestNullDateTimeObject()
        {
            var obj = new TestClass { Expiry = null };
            var json = JsonConvert.SerializeObject(obj);
            var obj2 = JsonConvert.DeserializeObject<TestClass>(json);

            Assert.Equal(obj.Expiry, obj2.Expiry);
        }

        public class TestClass
        {
            [JsonConverter(typeof(DateTimeOffsetConverter))]
            public DateTimeOffset? Expiry { get; set; }
        }
    }
}
