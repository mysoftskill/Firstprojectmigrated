// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.PrivacyRequest
{
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class CloudInstanceMapperTest
    {
        [TestMethod]
        [DataRow(null, null)]
        [DataRow(CloudInstanceType.INT, null /* int is internal for us and not meant to send to PCF */)]
        [DataRow(CloudInstanceType.AzurePPE, null /* pcf doesn't use azure ppe */)]
        [DataRow(CloudInstanceType.AzureTestInProd, "Public" /* Naming is confusing on this one... Azure PPE didn't exist initially, so we used this */)]
        [DataRow(CloudInstanceType.PublicProd, "Public" /* aka PROD */)]
        [DataRow(CloudInstanceType.CNAzureMooncake, "CN.Azure.Mooncake")]
        [DataRow(CloudInstanceType.CNO365Gallatin, "CN.O365.Gallatin")]
        [DataRow(CloudInstanceType.USAzureFairfax, "US.Azure.Fairfax")]
        public void ShouldMapCorrectly(CloudInstanceType? cloudInstanceType, string expectedValue)
        {
            Assert.AreEqual(expectedValue, cloudInstanceType.ToPcfCloudInstance());
        }
    }
}
