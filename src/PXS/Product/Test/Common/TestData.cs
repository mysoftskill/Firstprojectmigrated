// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Test.Common
{
    using System;

    using Microsoft.Membership.MemberServices.Configuration;

    using Moq;

    public static class TestData
    {
        public const string IntAadS2STargetAudience = "705363a0-5817-47fb-ba32-59f47ce80bb7";

        public const string IntMsaOathEndpoint = "https://login.live-int.com/pksecure/oauth20_clientcredentials.srf";

        public const string IntS2STargetScope = "pxs.api.account.microsoft-int.com";

        public const string PpeS2STargetScope = "pxs.api.account.microsoft-ppe.com";

        public const string ProdMsaOathEndpoint = "https://login.live.com/pksecure/oauth20_clientcredentials.srf";

        public const string ProdS2STargetScope = "pxs.api.account.microsoft.com";

        /// <summary>
        ///     Value is registered @ ms.portal.azure.com in the AME tenant. App id: fe2a584c-c666-4d84-a644-cb4617eef3a4. Display Name: PXS Test
        ///     https://ms.portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/fe2a584c-c666-4d84-a644-cb4617eef3a4/isMSAApp/
        /// </summary>
        public const string TestAadAppId = "fe2a584c-c666-4d84-a644-cb4617eef3a4";

        public const long TestSiteIdIntProd = 295750; // Site ID registered in MSM for MemberView Test Site

        public const long TestSiteIdPpe = 296553; // Site ID registered in MSM for MemberView Test Site

        public const string TestSiteName = "pxstest.api.account.microsoft.com"; // Site Name registered in MSM for Test Site

        // A test tenant id that triggers 400 error
        public const string TenantId400 = "16928717-F5F8-48A7-BD2C-86C3FF6246CC";

        // A test tenant id that triggers 403 error
        public const string TenantId403 = "10676020-90A0-479B-BD9C-999111A511DF";

        // A test tenant id that triggers 409 error
        public const string TenantId409 = "FCABAAE8-3FEB-4ED2-9C82-3E461F677212";

        // A set of know IDs for multi tenant collaboration testing. AadRvs controller in PartnerMock will use these values to determine what verifiers should be returned

        // A test user object id from home tenant of a non-complex org
        public const string HomeUserObjIdNonComplexOrg = "2FF95749-4B59-4EEB-A1C7-1D087E2BDBF6";

        // A test user object id from resource tenant of a non-complex org
        public const string ResourceUserObjIdNonComplexOrg = "3893F2C7-DD93-4AB0-BA3D-8F28D0CBBA38";

        // A test tenant id treated as Home tenant,
        // This is the tenant id of meepxs
        public const string HomeTenantId = "7BDB2545-6702-490D-8D07-5CC0A5376DD9";

        // A test tenant id treated as Resource tenant
        // This is the tenant id of meepxsresource
        public const string ResourceTenantId = "49B410FF-4D15-4BDF-B82D-4687AC464753";

        // A test target object id that triggers a 400 error in AadRvs mock
        public const string ObjectId400 = "e50035cb-e069-418b-8e84-d3c2878a1bcb";

        // A test target object id that triggers a 401 error in AadRvs mock
        public const string ObjectId401 = "8e4581d6-994b-48b8-8236-5e468c31e42f";

        // A test target object id that triggers a 403 error in AadRvs mock
        public const string ObjectId403 = "a6d7a58a-dc70-4b90-913f-6fc27e615016";

        // A test target object id that triggers a 404 error in AadRvs mock
        public const string ObjectId404 = "40518b3b-1f59-46b3-8c6d-629b6e41f2c2";

        // A test target object id that triggers a 405 error in AadRvs mock
        public const string ObjectId405 = "26992da4-a598-4547-adaa-f2b44a0bf6c7";

        // A test target object id that triggers a 409 error in AadRvs mock
        public const string ObjectId409 = "40c14d08-8640-49a2-8b73-749e42516751";

        // A test target object id that triggers a 429 error in AadRvs mock
        public const string ObjectId429 = "63644473-9b24-4891-9088-beffe91d8106";

        public static readonly X509Certificate2Info IntTestS2SCertificate =
            new X509Certificate2Info(
                subject: "CN=pxstest-s2s.api.account.microsoft-int.com",
                issuer: null,
                fileName: null,
                password: null,
                thumbprint: null);
        // This is configured in AKV: https://adgcs-cloudtest-kv.vault.azure.net, Secret Name: pcf-sts-onecert
        // The Cert needs AllowedList in a few places.
        // 1. AAD Test app: https://ms.portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Credentials/appId/8374d0f0-30e7-439c-934f-74b3ce09ef38
        // 2. msm.live.com:, Site ID: 295750, Manage Certificates -> Configuration: INT -> Certificate Purpose: IDSAPI / Server-Server Authentication
        // 3. AAD App id: feb76379-5080-4b88-86d0-7bef3558d507 found in tenant 'meepxs'
        // 4. AAD App id: 31e2ae73-1a3f-4104-9868-4007cc2ee6ce found in tenant 'meepxsresource'
        // NOTE: Highly recommend the AAD Apps get setup to use SNI auth to avoid manual public key AllowedList.
        // Instructions at https://aadwiki.windows-int.net/index.php?title=Subject_Name_and_Issuer_Authentication
        public static readonly X509Certificate2Info CloudTestCertificate =
            new X509Certificate2Info(
                subject: "CN=cloudtest.privacy.microsoft-int.ms",
                issuer: null,
                fileName: null,
                password: null,
                thumbprint: null);

        public static readonly X509Certificate2Info PpeTestS2SCertificate =
            new X509Certificate2Info(
                subject: "CN=pxstest-s2s.api.account.microsoft-ppe.com",
                issuer: null,
                fileName: null,
                password: null,
                thumbprint: null);

        public static readonly X509Certificate2Info ProdTestS2SCertificate =
            new X509Certificate2Info(
                subject: "CN=pxstest-s2s.api.account.microsoft.com",
                issuer: null,
                fileName: null,
                password: null,
                thumbprint: null);

        public static readonly Uri TestSiteUri = new Uri("https://pxstest.api.account.microsoft.com"); // Site URI registered in MSM for Test Site

        /// <summary>
        ///     Configuration used to retrieve RPS tickets (user tickets) in INT.
        ///     Retries the user tickets on behalf of MemberView Test Site.
        /// </summary>
        public static IRpsConfiguration IntUserTicketConfiguration(string authPolicy = "MBI_SSL")
        {
            var mockConfig = new Mock<IRpsConfiguration>(MockBehavior.Strict);
            mockConfig.SetupGet(m => m.Environment).Returns(RpsEnvironment.Int);
            mockConfig.SetupGet(m => m.SiteId).Returns(TestSiteIdIntProd.ToString());
            mockConfig.SetupGet(m => m.SiteName).Returns(TestSiteName);
            mockConfig.SetupGet(m => m.SiteUri).Returns(TestSiteUri);
            mockConfig.SetupGet(m => m.AuthPolicy).Returns(authPolicy);
            return mockConfig.Object;
        }
    }
}
