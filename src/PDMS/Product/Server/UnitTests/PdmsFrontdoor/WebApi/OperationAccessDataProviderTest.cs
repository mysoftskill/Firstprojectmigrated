namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using System.IO;
    using System.Linq;

    using Xunit;

    public class OperationAccessDataProviderTest
    {
        [Theory(DisplayName = "When given an applicationId, then return an array of APIs.")]
        [InlineData("789012", new[] { "V1.ReadAllDataAgents", "V1.ReadAllDataAssets", "V1.ReadAllCapabilities" })]
        [InlineData("123456", new string[0])]
        [InlineData("345678", null)]
        public void VerifyApiArrays(string applicationId, string[] expectedApis)
        {
            var data = @"
                <accessList>
                    <partner id='123456' name='Friendly name'>
                        <apis>
                        </apis>
                    </partner>
                    <partner id='789012' name='Friendly name1'>
                        <apis>
                            <api>V1.ReadAllDataAgents</api>
                            <api>V1.ReadAllDataAssets</api>
                            <api>V1.ReadAllCapabilities</api>
                        </apis>
                    </partner>
                </accessList>";

            var sourceFilePath = Path.GetTempFileName();
            File.WriteAllText(sourceFilePath, data);
            
            var operationAccessProvider = new OperationAccessProvider(sourceFilePath);

            var operationAccessPermission = operationAccessProvider.GetAccessPermissions(applicationId);

            if (expectedApis == null)
            {
                Assert.Null(operationAccessPermission);
            }
            else
            {
                Assert.True(expectedApis.SequenceEqual(operationAccessPermission.AllowedOperations));
            }
        }

        [Theory(DisplayName = "When the access list is empty, then return null for the request.")]
        [InlineData("123456")]
        public void VerifyApiArrays_EmptyList(string applicationId)
        {
            var data = @"
                <accessList>
                </accessList>";

            var sourceFilePath = Path.GetTempFileName();
            File.WriteAllText(sourceFilePath, data);

            var operationAccessProvider = new OperationAccessProvider(sourceFilePath);

            var operationAccessPermission = operationAccessProvider.GetAccessPermissions(applicationId);

            Assert.Null(operationAccessPermission);
        }
    }
}