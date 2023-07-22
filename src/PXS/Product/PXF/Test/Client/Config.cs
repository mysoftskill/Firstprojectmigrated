// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.TestClient
{
    using System;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Moq;

    /// <summary>
    /// Configuration
    /// </summary>
    public class Config
    {
        public Uri ServiceEndpoint { get; set; }

        public long SiteId { get; set; }

        public string TargetSite { get; set; }

        public Uri MsaOathEndpoint { get; set; }

        public X509Certificate2Info S2SCertificateInfo { get; set; }

        public IRpsConfiguration RpsConfiguration { get; set; }

        public FamilyModel.FamilyClientConfiguration FamilyClientConfiguration { get; set; }

        public Config(Options options)
        {
            Uri serviceEndpointUri = null;

            if (!string.IsNullOrWhiteSpace(options.ServiceEndpointUri))
            {
                serviceEndpointUri = CreateServiceEndpointUri(options.ServiceEndpointUri);
            }

            switch (options.Environment)
            {
                case Environment.DEV:
                    this.ServiceEndpoint = new Uri("https://127.0.0.1/");
                    this.SiteId = TestData.TestSiteIdIntProd;
                    this.TargetSite = TestData.IntS2STargetScope;
                    this.MsaOathEndpoint = new Uri(TestData.IntMsaOathEndpoint);
                    this.S2SCertificateInfo = TestData.CloudTestCertificate;
                    this.RpsConfiguration = this.CreateRpsConfiguration(RpsEnvironment.Int);
                    this.FamilyClientConfiguration = FamilyModel.IntFamilyClientConfiguration.Value;
                    break;
                case Environment.INT:
                    //
                    // When you use an ip address you may need this:
                    //                 PxsTestClient.exe -s true
                    //
                    //this.ServiceEndpoint = serviceEndpointUri ?? new Uri("https://corp.pxs.api.account.microsoft-int.com:81/");
                    // int sandbox in cy2 - https://52.161.27.146:443/
                    //this.ServiceEndpoint = serviceEndpointUri ?? new Uri("https://52.161.27.146:443/");
                    // int sandbox in bn1 - https://40.123.48.41:81/
                    this.ServiceEndpoint = serviceEndpointUri ?? new Uri("https://40.123.48.41:81/");
                    this.SiteId = TestData.TestSiteIdIntProd;
                    this.TargetSite = TestData.IntS2STargetScope;
                    this.MsaOathEndpoint = new Uri(TestData.IntMsaOathEndpoint);
                    this.S2SCertificateInfo = TestData.CloudTestCertificate;
                    this.RpsConfiguration = this.CreateRpsConfiguration(RpsEnvironment.Int);
                    this.FamilyClientConfiguration = FamilyModel.IntFamilyClientConfiguration.Value;
                    break;
                case Environment.PPE:
                    // example of start.bat:
                    // PxsTestClient.exe -o GetExportStatus -a 170526173142539204d3d3356bf6 -e PPE -u privacy-watchdog10@outlook.com -p <the password>
                    // example of prorab:
                    // prorab -c add -m EAP010230097140 -cluster bn1 -n pxstestclient.youralias -d 120 -pdb  -r 240 -s D:\src\Git\MEE.Privacy.Experience.Svc\Bin\Debug\x64\PrivacyExperienceTestClient
                    this.ServiceEndpoint = serviceEndpointUri ?? new Uri("https://pxs.api.account.microsoft-ppe.com/");
                    this.SiteId = TestData.TestSiteIdPpe;
                    this.TargetSite = TestData.PpeS2STargetScope;
                    this.MsaOathEndpoint = new Uri(TestData.ProdMsaOathEndpoint);
                    this.S2SCertificateInfo = TestData.PpeTestS2SCertificate;
                    this.RpsConfiguration = this.CreateRpsConfiguration(RpsEnvironment.Prod);
                    this.FamilyClientConfiguration = FamilyModel.PpeFamilyClientConfiguration.Value;
                    break;
                case Environment.PROD:
                    this.ServiceEndpoint = serviceEndpointUri ?? new Uri("https://pxs.api.account.microsoft.com/");
                    this.SiteId = TestData.TestSiteIdIntProd;
                    this.TargetSite = TestData.ProdS2STargetScope;
                    this.MsaOathEndpoint = new Uri(TestData.ProdMsaOathEndpoint);
                    this.S2SCertificateInfo = TestData.ProdTestS2SCertificate;
                    this.RpsConfiguration = this.CreateRpsConfiguration(RpsEnvironment.Prod);
                    this.FamilyClientConfiguration = FamilyModel.ProdFamilyClientConfiguration.Value;
                    break;
                default:
                    throw new InvalidOperationException("environment not supported: " + options.Environment);
            }
        }

        private static Uri CreateServiceEndpointUri(string serviceEndpoint)
        {
            try
            {
                return new Uri(serviceEndpoint);
            }
            catch (UriFormatException)
            {
                return CreateServiceEndpointUri(IOHelpers.GetUserInputString("Invalid Uri specified. Please enter a valid uri:"));
            }
        }

        private IRpsConfiguration CreateRpsConfiguration(RpsEnvironment rpsEnvironment)
        {
            Mock<IRpsConfiguration> mockConfig = new Mock<IRpsConfiguration>(MockBehavior.Strict);
            mockConfig.SetupGet(m => m.Environment).Returns(rpsEnvironment);
            mockConfig.SetupGet(m => m.SiteId).Returns(this.SiteId.ToString);
            mockConfig.SetupGet(m => m.SiteName).Returns(TestData.TestSiteName);
            mockConfig.SetupGet(m => m.SiteUri).Returns(TestData.TestSiteUri);
            mockConfig.SetupGet(m => m.AuthPolicy).Returns("MBI_SSL");
            return mockConfig.Object;
        }
    }
}