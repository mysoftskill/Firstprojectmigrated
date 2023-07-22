namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using System;

    using Microsoft.PrivacyServices.Testing;

    using Newtonsoft.Json;

    using Xunit;

    public class EnumTolerantConverterTest
    {
        [Flags]
        [JsonConverter(typeof(EnumTolerantConverter<Test>))]
        public enum Test
        {
            A = 1 << 0,
            B = 1 << 1,
            C = 1 << 2,
        }

        [Fact(DisplayName = "When given a recognized flags value, then parse properly.")]
        public void VerifyRecognizedFlagsValue()
        {
            var value = JsonConvert.DeserializeObject<Test>("\"A,b\"", SerializerSettings.Instance);
            Assert.Equal(Test.A | Test.B, value);
        }

        [Theory(DisplayName = "When given a partially recognized flags value, then parse properly."), AutoMoqData]
        public void VerifyPartialRecognizedFlagsValue(string enumValue)
        {
            var value = JsonConvert.DeserializeObject<Test>($"\"A,{enumValue},B\"", SerializerSettings.Instance);
            Assert.Equal(Test.A | Test.B, value);
        }

        [Fact(DisplayName = "When given an empty object with a nullable flag property, then parse properly.")]
        public void VerifyEmptyObjectWithNullableFlagProperty()
        {
            var value = JsonConvert.DeserializeObject<Container>("{}");
            Assert.Null(value.Test);
        }

        [Fact(DisplayName = "When given an object with nullable flag property set to be null, then parse properly.")]
        public void VerifyObjectWithNullFlagProperty()
        {
            var value = JsonConvert.DeserializeObject<Container>("{\"test\": null}");
            Assert.Null(value.Test);
        }

        [Fact(DisplayName = "When given an object with nullable flag property set to be a valid value, then parse properly.")]
        public void VerifyObjectWithValidFlagProperty()
        {
            var value = JsonConvert.DeserializeObject<Container>("{\"test\": \"A\"}");
            Assert.Equal(Test.A, value.Test);
        }

        [Theory(DisplayName = "When given an integer, then parse properly.")]
        [InlineAutoMoqData("1", Test.A)]
        [InlineAutoMoqData("3", Test.A | Test.B)]
        [InlineAutoMoqData("50", (Test)50)] // Unknown should just pass through.
        public void VerifyIntegerValue(string json, Test expectedResult)
        {
            var value = JsonConvert.DeserializeObject<Test>(json, SerializerSettings.Instance);
            Assert.Equal(expectedResult, value);
        }
        
        public class Container
        {
            public Test? Test { get; set; }
        }
    }
}