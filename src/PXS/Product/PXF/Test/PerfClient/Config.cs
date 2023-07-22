// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.PerfClient
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
                    this.ServiceEndpoint = serviceEndpointUri ?? new Uri("https://127.0.0.1/");
                    this.SiteId = TestData.TestSiteIdIntProd;
                    this.TargetSite = TestData.IntS2STargetScope;
                    this.MsaOathEndpoint = new Uri(TestData.IntMsaOathEndpoint);
                    this.S2SCertificateInfo = TestData.IntTestS2SCertificate;
                    this.RpsConfiguration = this.CreateRpsConfiguration(RpsEnvironment.Int);
                    this.FamilyClientConfiguration = FamilyModel.IntFamilyClientConfiguration.Value;
                    this.TestUserFileName = "Users\\IntUsers.txt";
                    break;
                case Environment.INT:
                    this.ServiceEndpoint = serviceEndpointUri ?? new Uri("https://pxs.api.account.microsoft-int.com");
                    this.SiteId = TestData.TestSiteIdIntProd;
                    this.TargetSite = TestData.IntS2STargetScope;
                    this.MsaOathEndpoint = new Uri(TestData.IntMsaOathEndpoint);
                    this.S2SCertificateInfo = TestData.IntTestS2SCertificate;
                    this.RpsConfiguration = this.CreateRpsConfiguration(RpsEnvironment.Int);
                    this.FamilyClientConfiguration = FamilyModel.IntFamilyClientConfiguration.Value;
                    this.TestUserFileName = "Users\\IntUsers.txt";
                    break;
                case Environment.PPE:
                    this.ServiceEndpoint = serviceEndpointUri ?? new Uri("https://pxs.api.account.microsoft-ppe.com");
                    this.SiteId = TestData.TestSiteIdPpe;
                    this.TargetSite = TestData.PpeS2STargetScope;
                    this.MsaOathEndpoint = new Uri(TestData.ProdMsaOathEndpoint);
                    this.S2SCertificateInfo = TestData.PpeTestS2SCertificate;
                    this.RpsConfiguration = this.CreateRpsConfiguration(RpsEnvironment.Prod);
                    this.FamilyClientConfiguration = FamilyModel.ProdFamilyClientConfiguration.Value;
                    this.TestUserFileName = "Users\\ProdUsers.txt";
                    break;
                case Environment.PROD:
                    this.ServiceEndpoint = serviceEndpointUri ?? new Uri("https://pxs.api.account.microsoft.com/");
                    this.SiteId = TestData.TestSiteIdIntProd;
                    this.TargetSite = TestData.ProdS2STargetScope;
                    this.MsaOathEndpoint = new Uri(TestData.ProdMsaOathEndpoint);
                    this.S2SCertificateInfo = TestData.ProdTestS2SCertificate;
                    this.RpsConfiguration = this.CreateRpsConfiguration(RpsEnvironment.Prod);
                    this.FamilyClientConfiguration = FamilyModel.ProdFamilyClientConfiguration.Value;
                    this.TestUserFileName = "Users\\ProdUsers.txt";
                    break;
                default:
                    throw new InvalidOperationException("environment not supported: " + options.Environment);
            }
        }

        public string TestUserFileName { get; set; }

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