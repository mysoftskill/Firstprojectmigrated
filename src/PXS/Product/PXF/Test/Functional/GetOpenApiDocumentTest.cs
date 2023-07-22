// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    /// <summary>
    ///     GetOpenApiDocumentTest
    /// </summary>
    [TestClass]
    public class GetOpenApiDocumentTest : TestBase
    {
        /// <summary>
        /// Ensures that the OpenApi document page works.
        /// </summary>
        [TestMethod, TestCategory("FCT")]
        public async Task OpenApiDocument()
        {
            using (HttpClient client = new HttpClient())
            {
                    
                    string uri = $"{TestConfiguration.ServiceEndpoint.Value}/v1/openapi";
                    var response = await client.GetAsync(uri);
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Assert.IsNotNull(content);

                    // A few simple checks that the content is what we expect
                    Assert.IsTrue(content.Contains("openapi"));
                    Assert.IsTrue(content.Contains("https://pxs.api.account.microsoft.com"));
            }
        }
    }
}
