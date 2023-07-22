// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.UnitTests.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Logging;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataSubjectRightsRequestExtendedTests
    {
        [DataTestMethod]
        [DynamicData(nameof(TestInputDataSubjectRightsRequest))]
        public void ShouldGenerateExpectedClass(
            RequestType requestType,
            IPrivacySubject expectedSubject,
            bool expectedIsTestRequest,
            string expectedRequesterValue,
            string expectedCloudInstance,
            string expectedPortal)
        {
            PrivacyRequest expectedRequest = null;
            switch (requestType)
            {
                case RequestType.Delete:
                    expectedRequest = new DeleteRequest();
                    expectedRequest.RequestType = RequestType.Delete;
                    break;
                case RequestType.Export:
                    expectedRequest = new ExportRequest();
                    expectedRequest.RequestType = RequestType.Export;
                    break;
                case RequestType.AccountClose:
                    expectedRequest = new AccountCloseRequest();
                    expectedRequest.RequestType = RequestType.AccountClose;
                    break;
                case RequestType.AgeOut:
                    expectedRequest = new AgeOutRequest();
                    expectedRequest.RequestType = RequestType.AgeOut;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null);
            }

            Assert.IsNotNull(expectedRequest);
            expectedRequest.Subject = expectedSubject;

            switch (expectedSubject)
            {
                case AadSubject aadSubject:
                    expectedRequest.ControllerApplicable = false;
                    expectedRequest.ProcessorApplicable = true;
                    break;
                case DemographicSubject demographicSubject:
                case MicrosoftEmployee microsoftEmployeeSubject:
                    break;
                case DeviceSubject deviceSubject:
                    expectedRequest.ControllerApplicable = true;
                    expectedRequest.ProcessorApplicable = false;
                    break;
                case MsaSubject msaSubject:
                    expectedRequest.ControllerApplicable = true;
                    expectedRequest.ProcessorApplicable = false;
                    break;
            }

            expectedRequest.CloudInstance = expectedCloudInstance;
            expectedRequest.Portal = expectedPortal;
            expectedRequest.Requester = expectedRequesterValue;
            expectedRequest.IsTestRequest = expectedIsTestRequest;

            var actualRequest = new DataSubjectRightsRequestExtended(expectedRequest);

            Assert.AreEqual(expectedRequest.RequestType.ToString(), actualRequest.dataSubjectRightsRequest.RequestType);
            Assert.AreEqual(expectedCloudInstance ?? string.Empty, actualRequest.dataSubjectRightsRequest.CloudInstance);
            Assert.AreEqual(expectedRequest.ControllerApplicable, actualRequest.dataSubjectRightsRequest.ControllerApplicable);
            Assert.AreEqual(expectedRequest.ProcessorApplicable, actualRequest.dataSubjectRightsRequest.ProcessorApplicable);
            Assert.AreEqual(expectedPortal ?? string.Empty, actualRequest.dataSubjectRightsRequest.Portal);
            Assert.AreEqual(expectedRequesterValue ?? string.Empty, actualRequest.dataSubjectRightsRequest.Requester);
            Assert.AreEqual(expectedIsTestRequest, actualRequest.dataSubjectRightsRequest.IsTestRequest);

            if (expectedRequest.GetType() == typeof(AccountCloseRequest))
            {
                Assert.AreEqual(RequestType.AccountClose.ToString(), actualRequest.dataSubjectRightsRequest.RequestType);
            }
            else if (expectedRequest.GetType() == typeof(AgeOutRequest))
            {
                Assert.AreEqual(RequestType.AgeOut.ToString(), actualRequest.dataSubjectRightsRequest.RequestType);
            }
            else if (expectedRequest.GetType() == typeof(DeleteRequest))
            {
                Assert.AreEqual(RequestType.Delete.ToString(), actualRequest.dataSubjectRightsRequest.RequestType);
            }
            else if (expectedRequest.GetType() == typeof(ExportRequest))
            {
                Assert.AreEqual(RequestType.Export.ToString(), actualRequest.dataSubjectRightsRequest.RequestType);
            }

            switch (expectedRequest.Subject)
            {
                case AadSubject aadSubject:
                    Assert.AreEqual("AAD", actualRequest.dataSubjectRightsRequest.SubjectType);
                    Assert.AreEqual(typeof(AadSubject), expectedRequest.Subject.GetType());
                    Assert.AreEqual($"a:{((AadSubject)expectedRequest.Subject).ObjectId}", ((UserInfo)actualRequest.fillEnvelope.Target).Id);
                    break;
                case DemographicSubject demographicSubject:
                    Assert.AreEqual("Alternate", actualRequest.dataSubjectRightsRequest.SubjectType);
                    Assert.AreEqual(typeof(DemographicSubject), expectedRequest.Subject.GetType());
                    Assert.IsNull(actualRequest.fillEnvelope);
                    break;
                case MicrosoftEmployee microsoftEmployeeSubject:
                    Assert.AreEqual("Alternate", actualRequest.dataSubjectRightsRequest.SubjectType);
                    Assert.AreEqual(typeof(MicrosoftEmployee), expectedRequest.Subject.GetType());
                    Assert.IsNull(actualRequest.fillEnvelope);
                    break;
                case DeviceSubject deviceSubject:
                    Assert.AreEqual("Device", actualRequest.dataSubjectRightsRequest.SubjectType);
                    Assert.AreEqual(typeof(DeviceSubject), expectedRequest.Subject.GetType());
                    Assert.AreEqual($"g:{((DeviceSubject)expectedRequest.Subject).GlobalDeviceId}", ((DeviceInfo)actualRequest.fillEnvelope.Target).Id);
                    break;
                case MsaSubject msaSubject:
                    Assert.AreEqual("MSA", actualRequest.dataSubjectRightsRequest.SubjectType);
                    Assert.AreEqual(typeof(MsaSubject), expectedRequest.Subject.GetType());
                    Assert.AreEqual($"{((MsaSubject)expectedRequest.Subject).AsimovPuid}", ((UserInfo)actualRequest.fillEnvelope.Target).Id);
                    break;
            }

            // Ensure log doesn't throw.
            actualRequest.Log();
        }

        private static IEnumerable<object[]> TestInputDataSubjectRightsRequest =>
            new List<object[]>
            {
                new object[] { RequestType.AccountClose, CreateSubject("AAD"), true, "I-am-the-requester", "cloudName", "portalName" },
                new object[] { RequestType.AccountClose, CreateSubject("MSA"), false, string.Empty, string.Empty, string.Empty },
                new object[] { RequestType.AgeOut, CreateSubject("MSA"), false, null, null, null },
                new object[] { RequestType.Export, CreateSubject("AAD"), false, string.Empty, string.Empty, string.Empty },
                new object[] { RequestType.Export, CreateSubject("MSA"), false, string.Empty, string.Empty, string.Empty },
                new object[] { RequestType.Delete, CreateSubject("AAD"), false, string.Empty, string.Empty, string.Empty },
                new object[] { RequestType.Delete, CreateSubject("MSA"), false, string.Empty, string.Empty, string.Empty },
                new object[] { RequestType.Delete, CreateSubject("Device"), false, string.Empty, string.Empty, string.Empty },
                new object[] { RequestType.Delete, CreateSubject("Demographic"), false, string.Empty, string.Empty, string.Empty },
                new object[] { RequestType.Export, CreateSubject("Demographic"), false, string.Empty, string.Empty, string.Empty },
                new object[] { RequestType.Delete, CreateSubject("MicrosoftEmployee"), false, string.Empty, string.Empty, string.Empty },
                new object[] { RequestType.Export, CreateSubject("MicrosoftEmployee"), false, string.Empty, string.Empty, string.Empty }
            };

        private static IPrivacySubject CreateSubject(string subjectType)
        {
            Random random = new Random();

            string RandomString(int length)
            {
                const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                return new string(
                    Enumerable.Repeat(Chars, length)
                        .Select(s => s[random.Next(s.Length)]).ToArray());
            }

            switch (subjectType)
            {
                case "AAD":
                    return new AadSubject { ObjectId = Guid.NewGuid(), TenantId = Guid.NewGuid(), OrgIdPUID = new Random().Next() };

                case "MSA":
                    return new MsaSubject { Anid = RandomString(32), Puid = random.Next(), Cid = random.Next(), Opid = RandomString(23), Xuid = RandomString(5) };

                case "Device":
                    return new DeviceSubject { GlobalDeviceId = new Random().Next() };

                case "Demographic":
                    return new DemographicSubject { EmailAddresses = new[] { "someperson@somecompany.com" } };

                case "MicrosoftEmployee":
                    return new MicrosoftEmployee { EmployeeId = RandomString(32), Emails = new[] { "someperson@somecompany.com" }};
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(subjectType), subjectType, null);
            }
        }
    }
}
