namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using Ploeh.AutoFixture;

    using Xunit;

    public class SerializationTest
    {
        [Fact(DisplayName = "When serialized is called with large data, then truncate.")]
        public void VerifyTruncation()
        {
            var fixture = new Fixture();
            var data = fixture.CreateMany<TestObject>(5000);
            var result = Serialization.Serialize(data);
            Assert.StartsWith("[TRUNCATED]", result);
            Assert.Equal(Serialization.BuilderLength, result.Length);
        }

        public class TestObject
        {
            public string Value { get; set; }
        }
    }
}