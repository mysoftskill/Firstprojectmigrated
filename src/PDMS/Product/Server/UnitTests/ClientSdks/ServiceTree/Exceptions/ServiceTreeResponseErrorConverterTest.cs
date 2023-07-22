namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.Exceptions.UnitTest
{
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
    using Newtonsoft.Json;

    using Xunit;

    public class ServiceTreeResponseErrorConverterTest
    {
        [Fact(DisplayName = "When the minimal set of properties are returned, then parse successfully.")]
        public void VerifyMinimumProperties()
        {
            string Message = @"The team group with provided ID b8408de8-879f-4953-8951-438227458a09 does not exist.";
            string Details = "Microsoft.Tree.CommonServiceFunctionalities.Exceptions.TeamGroupNotFoundException: " + Message;
            var json = "{ \"Message\": \"" + Message + "\", \"Details\": \"" + Details + "\"}";
            var responseError = JsonConvert.DeserializeObject<ServiceTreeResponseError>(json, SerializerSettings.Instance);
            Assert.Equal(Details, responseError.Details);
            Assert.Equal(Message, responseError.Message);
        }
    }
}
