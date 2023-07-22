// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Controllers
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataPolicyOperationsControllerTests
    {
        [TestMethod]
        public void TestConvertPrivacyRequest()
        {
            Guid id = Guid.NewGuid();
            DateTimeOffset submittedTime = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset completedTime = DateTimeOffset.UtcNow;
            AadSubject subject = new AadSubject
            {
                ObjectId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                OrgIdPUID = long.MinValue
            };
            List<string> dataTypes = new List<string> { "dataType" };
            PrivacyRequestState state = PrivacyRequestState.Completed;
            PrivacyRequestType requestType = PrivacyRequestType.Delete;
            Uri destinationUri = new Uri("https://azurestorage");
            double progress = 1.0;

            PrivacyRequestStatus privacyRequestStatus = new PrivacyRequestStatus(id, requestType, submittedTime, completedTime, subject, dataTypes, null, state, destinationUri, progress);
            var result = DataPolicyOperationsController.ConvertPrivacyRequest(privacyRequestStatus);

            Assert.AreEqual(id.ToString(), result.Id);
            Assert.AreEqual(submittedTime, result.SubmittedDateTime);
            Assert.AreEqual(completedTime, result.CompletedDateTime);
            Assert.AreEqual(subject.ObjectId.ToString(), result.UserId);
            Assert.AreEqual(DataPolicyOperationStatus.Complete, result.Status);
            Assert.AreEqual(destinationUri.ToString(), result.StorageLocation);
            Assert.AreEqual(destinationUri.ToString(), result.StorageLocation);
            Assert.AreEqual(progress, result.Progress);
        }
    }
}
