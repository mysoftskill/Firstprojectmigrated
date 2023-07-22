// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public static class TestSetup
    {
        /// <summary>
        /// See Carbon Wiki for detailed instructions on initializing Titan machines for test execution 
        /// <![CDATA[https://microsoft.sharepoint.com/teams/osg_oss/mem/_layouts/OneNote.aspx?id=%2Fteams%2Fosg_oss%2Fmem%2FShared%20Documents%2FPlatform%20Services%2FCustomer%20Connection%2FCarbon%20Wiki%20%5BPublished%5D&wd=target%28Validation.one%7CD8208A78-C6EA-4F22-884E-4D9693370482%2FHow%20to%20Connect%20to%20AP%20Environment%20from%20Titan%20Machine%7C4BB176B1-CB7E-40D7-BFF9-2BE614DFC265%2F%29]]>
        /// </summary>
        /// <param name="context">The context.</param>
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            if (Environment.UserDomainName.ToUpper().Equals("PHX"))
            {
                RouteInitializer.AddRouteForTitanToAP();
            }
        }
    }
}