namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.UnitTest
{
    using System;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
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
            var value = JsonConvert.DeserializeObject<Test>("\"A,b\"");
            Assert.Equal(Test.A | Test.B, value);
        }

        [Theory(DisplayName = "When given a partially recognized flags value, then parse properly."), AutoMoqData]
        public void VerifyPartialRecognizedFlagsValue(string enumValue)
        {
            var value = JsonConvert.DeserializeObject<Test>($"\"A,{enumValue},B\"");
            Assert.Equal(Test.A | Test.B, value);
        }

        [Theory(DisplayName = "When given an integer, then parse properly.")]
        [InlineAutoMoqData("1", Test.A)]
        [InlineAutoMoqData("3", Test.A | Test.B)]
        [InlineAutoMoqData("50", (Test)50)] // Unknown should just pass through.
        public void VerifyIntegerValue(string json, Test expectedResult)
        {
            var value = JsonConvert.DeserializeObject<Test>(json);
            Assert.Equal(expectedResult, value);
        }
    }
}
