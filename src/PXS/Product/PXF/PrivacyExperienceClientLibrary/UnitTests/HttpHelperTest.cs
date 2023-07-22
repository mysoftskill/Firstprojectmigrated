// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.UnitTests
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// HttpHelperTest
    /// </summary>
    [TestClass]
    public class HttpHelperTest
    {
        [TestMethod]
        public async Task HandleHttpResponseAsyncSuccess()
        {
            var expectedResponseObject = new BrowseHistoryV1();
            var httpResponseMessage = new HttpResponseMessage();
            httpResponseMessage.Content = new ObjectContent(typeof(BrowseHistoryV1), expectedResponseObject, new JsonMediaTypeFormatter());

            var result = await HttpHelper.HandleHttpResponseAsync<BrowseHistoryV1>(httpResponseMessage);

            EqualityHelper.AreEqual(expectedResponseObject, result);
        }

        [ExpectedException(typeof(PrivacyExperienceClientException))]
        [TestMethod]
        public async Task HandleHttpResponseAsyncInvalidContentType()
        {
            var expectedResponseContent = "random error text";
            var expectedError = new Error(ErrorCode.Unknown, expectedResponseContent);

            var httpResponseMessage = new HttpResponseMessage();
            httpResponseMessage.Content = new StringContent(expectedResponseContent);

            try
            {
                var result = await HttpHelper.HandleHttpResponseAsync<Error>(httpResponseMessage);
            }
            catch (PrivacyExperienceClientException ex)
            {
                EqualityHelper.AreEqual(expectedError, ex.Error);
                throw;
            }

            Assert.Fail("An exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task HandleHttpResponseAsyncNullContent()
        {
            var httpResponseMessage = new HttpResponseMessage();
            try
            {
                var result = await HttpHelper.HandleHttpResponseAsync<Error>(httpResponseMessage);
            }
            catch (ArgumentNullException ex)
            {
                var expectedError = "Value cannot be null.\r\nParameter name: content";
                Assert.AreEqual(expectedError, ex.Message);
                throw;
            }

            Assert.Fail("An exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task HandleHttpResponseAsyncNullResponse()
        {
            await HttpHelper.HandleHttpResponseAsync<Error>(null);
        }
    }
}