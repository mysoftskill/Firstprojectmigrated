// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.ODataConfigs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpRequestExtensionsTests
    {
        [TestMethod]
        [DataRow("Post", RouteNames.VortexIngestionDeviceDeleteV1, "VortexIngestionDeviceDelete")] // Good Route and Method
        [DataRow("Put", RouteNames.VortexIngestionDeviceDeleteV1, ApiRouteMapping.DefaultApiName)] // Method No Match
        [DataRow("Post", RouteNames.VortexIngestionDeviceDeleteV1 + "NOMATCH", ApiRouteMapping.DefaultApiName)] // Route No Match
        [DataRow("Post", "users('b686bgg1-c8b1-4dc0-8cdf-7c274340f8fe')/" + ModelBuilder.ODataNamespace + ".exportPersonalData", ApiNames.ExportPersonalData)] // Check qualified Odata Route
        [DataRow("Post", "users('b686bgg1-c8b1-4dc0-8cdf-7c274340f8fe')/" + ModelBuilder.ODataNamespace + "2" + ".exportPersonalData", ApiRouteMapping.DefaultApiName)] // Check bad qualified Odata Route
        [DataRow("Post", "users('b686bgg1-c8b1-4dc0-8cdf-7c274340f8fe')/exportPersonalData", ApiNames.ExportPersonalData)] // Check unqualified Odata Route
        [DataRow("Post", "users('b686bgg1-c8b1-4dc0-8cdf-7c274340f8fe')/exportData", ApiRouteMapping.DefaultApiName)] // Check bad unqualified Odata Route

        public void GetApiNameTest(string method, string route, string expected)
        {
            Uri requestUri = new Uri($"https://www.test.com/" + route);
            Dictionary<string, HttpMethod> httpMethods = new Dictionary<string, HttpMethod>()
            {
                { "Delete", HttpMethod.Delete },
                { "Get", HttpMethod.Get },
                { "Post", HttpMethod.Post },
                { "Put", HttpMethod.Put }
            };
            HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethods[method], requestUri);
            string apiName = requestMessage.GetApiName();
            Assert.AreEqual(apiName, expected);
        }
    }
}