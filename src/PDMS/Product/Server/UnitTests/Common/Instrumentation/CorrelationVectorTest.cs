namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using Xunit;

    public class CorrelationVectorTest
    {
        [Fact(DisplayName = "When cv.Increment is called and no initial value is set, then return new incremented CV.")]
        public void VerifyIncrementWithoutSet()
        {
            var cv = new CorrelationVector();
            var result = cv.Increment();
            var incrementIndex = result.Length - 1;

            Assert.Equal("1", result.Substring(incrementIndex)); // Verify incremented.
        }

        [Fact(DisplayName = "When cv.Increment is called and an initial value is set, then return original value incremented.")]
        public void VerifyIncrementWithSet()
        {
            var cv = new CorrelationVector();
            cv.Set("test");
            Assert.Equal("test.1", cv.Increment());
        }

        [Fact(DisplayName = "When cv.Set is called after a cv.Increment, then do not alter CV value.")]
        public void VerifySetAfterIncrement()
        {
            var cv = new CorrelationVector();
            var result = cv.Increment();
            cv.Set("test");
            Assert.False(cv.Increment().StartsWith("test"));
        }

        [Fact(DisplayName = "When cv.Set is called twice, then return first value.")]
        public void VerifyDoubleSet()
        {
            var cv = new CorrelationVector();
            cv.Set("test");
            cv.Set("change");
            Assert.Equal("test.1", cv.Increment());
        }

        [Fact(DisplayName = "When cv.Get() is called, then return without incrementing.")]
        public void VerifyGet()
        {
            var cv = new CorrelationVector();
            cv.Set("test");
            Assert.Equal("test.0", cv.Get());
        }

        [Fact(DisplayName = "When cv.GetRaw() is called, then return raw value without incrementing.")]
        public void VerifyGetRaw()
        {
            var cv = new CorrelationVector();
            var data = cv.GetRaw();
            Assert.IsType<Microsoft.CommonSchema.Services.Logging.CorrelationVector>(data);
            Assert.EndsWith(".0", data.ToString());
        }
    }
}
