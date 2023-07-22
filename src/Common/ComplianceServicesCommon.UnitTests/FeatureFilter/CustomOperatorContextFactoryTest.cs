namespace Microsoft.Azure.ComplianceServices.Common.UnitTests.FeatureFilter
{
    using Xunit;
    using static Microsoft.Azure.ComplianceServices.Common.CustomOperatorContextFactory;

    public class CustomOperatorContextFactoryTest
    {
        [Theory]
        [InlineData("DataKey","DataVal","Operation","Caller","DataVal_DataKey_Operation_Caller")]
        [InlineData(null, "DataVal", "Operation", "Caller", "DataVal_Operation_Caller")]
        [InlineData("DataKey", null, "Operation", "Caller", "_DataKey_Operation_Caller")]
        [InlineData("DataKey", "DataVal", null, "Caller", "DataVal_DataKey_Caller")]
        [InlineData("DataKey", "DataVal", "Operation", null, "DataVal_DataKey_Operation")]
        public void VerifyCustomOperatorContextToString(string key, string Value, 
                        string IncomingOperationName, string IncomingCallerName, string result)
        {
            CustomOperatorContext context = new CustomOperatorContext
            {
                Value = Value,
                Key = key,
                IncomingCallerName = IncomingCallerName,
                IncomingOperationName = IncomingOperationName
            };

            Assert.True(context.ToString() == result);
        }
    }
}
