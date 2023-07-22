using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    [TestClass]
    public class SubjectConvertorTest
    {
        [TestMethod]
        public void SubjectConvertorTestAadSubject2()
        {
            AadSubject2 expectedSubject = new AadSubject2() {
                TenantIdType = TenantIdType.Home,
                ObjectId = Guid.Parse("5674b2a1-61ac-4c05-86c7-2b7d4843851c"),
                HomeTenantId = Guid.Parse("a80aafff-2a2a-4099-8fd4-bcbe5bcaff6d"),
                TenantId = Guid.Parse("a80aafff-2a2a-4099-8fd4-bcbe5bcaff6d"),
                OrgIdPUID = 1154012207270968108
            };

            dynamic v2Subject = new
            {
                aadObjectId = "5674b2a1-61ac-4c05-86c7-2b7d4843851c",
                aadResourceTenantId = "a80aafff-2a2a-4099-8fd4-bcbe5bcaff6d",
                aadPuid = "1154012207270968108",
                aadTenantIdType = "Home",
                aadHomeTenantId = "a80aafff-2a2a-4099-8fd4-bcbe5bcaff6d"
            };

            AadSubject2 actualSubject = SubjectConverter.GetV1SubjectForValidation(JToken.FromObject(v2Subject));

            Assert.AreEqual(expectedSubject.TenantIdType , actualSubject.TenantIdType);
            Assert.AreEqual(expectedSubject.ObjectId, actualSubject.ObjectId);
            Assert.AreEqual(expectedSubject.HomeTenantId, actualSubject.HomeTenantId);
            Assert.AreEqual(expectedSubject.TenantId, actualSubject.TenantId);
            Assert.AreEqual(expectedSubject.OrgIdPUID, actualSubject.OrgIdPUID);
        }

        [TestMethod]
        public void SubjectConvertorTestMsftEmployeeSubject()
        {
            dynamic v2Subject = new
            {
                demographicMsftEmployeeEmails = "[\"test@success.com\",\"test2@success.com\"]",
                demographicMsftEmployeeId = "1234512",
                demographicMsftEmployeeStartDate = "2012-03-01T00:00:00Z",
                demographicMsftEmployeeEndDate = "2016-03-01T00:00:00Z"
            };

            var expectedMicrosoftEmployeeSubject = new MicrosoftEmployee()
            {
                Emails = new List<string>()
                {
                    "test@success.com",
                    "test2@success.com"
                },
                EmployeeId = "1234512",
                StartDate = DateTime.Parse("2012-03-01T00:00:00Z"),
                EndDate = DateTime.Parse("2016-03-01T00:00:00Z")
            };

            MicrosoftEmployee microsoftEmployeeSubject = SubjectConverter.GetV1SubjectForValidation(JToken.FromObject(v2Subject));
            
            CollectionAssert.AreEqual((System.Collections.ICollection)expectedMicrosoftEmployeeSubject.Emails, (System.Collections.ICollection)microsoftEmployeeSubject.Emails);
            Assert.AreEqual(expectedMicrosoftEmployeeSubject.EmployeeId, microsoftEmployeeSubject.EmployeeId);
            Assert.AreEqual(expectedMicrosoftEmployeeSubject.StartDate, microsoftEmployeeSubject.StartDate);
            Assert.AreEqual(expectedMicrosoftEmployeeSubject.EndDate, microsoftEmployeeSubject.EndDate);
        }
        
        [TestMethod]
        public void SubjectConvertorTestDemographicSubject()
        {
            dynamic v2Subject = new
            {
                demographicEmailAddresses = "[\"test@success.com\"]",
                demographicNames = "[\"test\"]",
                demographicPhoneNumbers = "[\"123456789\"]"
            };

            var expectedDemographicSubject = new DemographicSubject()
            {
                EmailAddresses = new List<string>()
                {
                    "test@success.com"
                },
                Names = new List<string>()
                {
                    "test"
                },
                PhoneNumbers = new List<string>()
                {
                    "123456789"
                }
            };

            DemographicSubject demographicSubject = SubjectConverter.GetV1SubjectForValidation(JToken.FromObject(v2Subject));
            CollectionAssert.AreEqual((System.Collections.ICollection) expectedDemographicSubject.EmailAddresses, (System.Collections.ICollection) demographicSubject.EmailAddresses);
            CollectionAssert.AreEqual((System.Collections.ICollection) expectedDemographicSubject.Names, (System.Collections.ICollection) demographicSubject.Names);
            CollectionAssert.AreEqual((System.Collections.ICollection) expectedDemographicSubject.PhoneNumbers, (System.Collections.ICollection) demographicSubject.PhoneNumbers);
        }

        [TestMethod]
        public void SubjectConvertorTestMsaSubject()
        {
            MsaSubject expectedSubject = new MsaSubject()
            {
                Anid = "5674b2a1-61ac-4c05-86c7-2b7d4843851c",
                Cid = 5554012207270968108,
                Opid = "a80aafff-2a2a-4099-8fd4-bcbe5bcaff6d",
                Puid = 5555552207270968108,
                Xuid = "b80aedff-2a2a-4099-8fd4-bcbe5bcaff6d"
            };

            dynamic v2Subject = new
            {
                msaAnid = "5674b2a1-61ac-4c05-86c7-2b7d4843851c",
                msaCid = "5554012207270968108",
                msaOpid = "a80aafff-2a2a-4099-8fd4-bcbe5bcaff6d",
                msaPuid = "5555552207270968108",
                msaXuid = "b80aedff-2a2a-4099-8fd4-bcbe5bcaff6d"
            };

            MsaSubject actualSubject = SubjectConverter.GetV1SubjectForValidation(JToken.FromObject(v2Subject));

            Assert.AreEqual(expectedSubject.Anid, actualSubject.Anid);
            Assert.AreEqual(expectedSubject.Cid, actualSubject.Cid);
            Assert.AreEqual(expectedSubject.Opid, actualSubject.Opid);
            Assert.AreEqual(expectedSubject.Puid, actualSubject.Puid);
            Assert.AreEqual(expectedSubject.Xuid, actualSubject.Xuid);
        }

        [TestMethod]
        public void SubjectConvertorTestDeviceSubject()
        {
            DeviceSubject expectedSubject = new DeviceSubject()
            {
                GlobalDeviceId = 4444442207270968108
            };

            dynamic v2Subject = new
            {
                globalDeviceId = "4444442207270968108"
            };

            DeviceSubject actualSubject = SubjectConverter.GetV1SubjectForValidation(JToken.FromObject(v2Subject));
            Assert.AreEqual(expectedSubject.GlobalDeviceId, actualSubject.GlobalDeviceId);
        }

        [TestMethod]
        public void SubjectConvertorTestEdgeBrowserSubject()
        {
            EdgeBrowserSubject expectedSubject = new EdgeBrowserSubject()
            {
                EdgeBrowserId = 3333332207270968108
            };

            dynamic v2Subject = new
            {
                edgeBrowserId = "3333332207270968108"
            };

            EdgeBrowserSubject actualSubject = SubjectConverter.GetV1SubjectForValidation(JToken.FromObject(v2Subject));
            Assert.AreEqual(expectedSubject.EdgeBrowserId, actualSubject.EdgeBrowserId);
        }

        [TestMethod]
        public void SubjectConvertorTestNonWindowsDeviceSubject()
        {
            NonWindowsDeviceSubject expectedSubject = new NonWindowsDeviceSubject()
            {
                MacOsPlatformDeviceId = Guid.Parse("5674b2a1-61ac-4c05-86c7-2b7d4843851c")
            };

            dynamic v2Subject = new
            {
                macOsPlatformDeviceId = "5674b2a1-61ac-4c05-86c7-2b7d4843851c"
            };

            NonWindowsDeviceSubject actualSubject = SubjectConverter.GetV1SubjectForValidation(JToken.FromObject(v2Subject));
            Assert.AreEqual(expectedSubject.MacOsPlatformDeviceId, actualSubject.MacOsPlatformDeviceId);
        }
    }
}
