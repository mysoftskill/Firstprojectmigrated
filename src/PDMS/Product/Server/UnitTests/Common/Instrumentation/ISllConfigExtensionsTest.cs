namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using System;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;

    using Moq;

    using Xunit;

    public class ISllConfigExtensionsTest
    {
        [Theory(DisplayName = "Verify parsing valid and invalid event levels.")]
        [InlineData("bad", "")]
        [InlineData("Warning", "Warning")]
        [InlineData("error", "Error")]
        [InlineData("bad,Error,bad", "Error")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void VerifyParsingEventLevels(string input, string expected)
        {
            var config = new Mock<ISllConfig>();
            config.SetupGet(m => m.EventLevels).Returns(input?.Split(','));

            var values = config.Object.ParsedEventLevels();

            Assert.Equal(expected, string.Join(",", values));
        }

        [Fact(DisplayName = "When the LocalLogDir has an environment variable in it, then expand that value.")]
        public void VerifyParsingLocalLogDir()
        {
            var config = new Mock<ISllConfig>();
            config.SetupGet(m => m.LocalLogDir).Returns(@"%test%\folder");

            Environment.SetEnvironmentVariable("test", @"c:");

            var value = config.Object.ParsedLocalLogDir();

            Assert.Equal(@"c:\folder", value);
        }
    }
}