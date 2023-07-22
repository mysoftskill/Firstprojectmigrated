// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.GraphApis
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.GraphApis;
    using Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    /// <summary>
    ///     GetDataPolicyOperationTest
    /// </summary>
    [TestClass]
    public class GetDataPolicyOperationTest : GraphApiTestBase
    {
        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        public async Task GetDataPolicyOperationByIdShouldReturnResult()
        {
            Guid commandId = Guid.NewGuid();
            using (HttpResponseMessage httpResponseMessage =
                await CallGetAsync($"dataPolicyOperations('{commandId}')", TestIdentityHomeTenant).ConfigureAwait(false))
            {
                Assert.IsNotNull(httpResponseMessage);
                string content = httpResponseMessage.Content != null ? await (httpResponseMessage.Content?.ReadAsStringAsync()).ConfigureAwait(false) : string.Empty;
                var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                Assert.IsTrue(httpResponseMessage.IsSuccessStatusCode, responseDetails);
                Assert.AreEqual(HttpStatusCode.OK, httpResponseMessage.StatusCode, responseDetails);
                Assert.IsTrue(!string.IsNullOrWhiteSpace(content), responseDetails);
                DataPolicyOperation commandStatusResponse = JsonConvert.DeserializeObject<DataPolicyOperation>(content);
                Assert.IsNotNull(commandStatusResponse, responseDetails);
                Assert.AreEqual(commandId.ToString(), commandStatusResponse.Id, responseDetails);
                Console.WriteLine(responseDetails);
            }
        }

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        public async Task GetDataPolicyOperationsShouldSucceed()
        {
            using (HttpResponseMessage httpResponseMessage = await CallGetAsync("dataPolicyOperations", TestIdentityHomeTenant).ConfigureAwait(false))
            {
                // Assert: Validate no error returned.
                Assert.IsNotNull(httpResponseMessage);
                string content = httpResponseMessage.Content != null ? await (httpResponseMessage.Content?.ReadAsStringAsync()).ConfigureAwait(false) : string.Empty;
                Assert.IsTrue(httpResponseMessage.IsSuccessStatusCode, $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}");
            }
        }
    }
}
