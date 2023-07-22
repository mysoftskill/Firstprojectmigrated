// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.Common
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class CdpEvent2HelperTests : SharedTestFunctions
    {
        private CDPEvent2 cdpEvent2;

        private CdpEvent2Helper cdpEvent2Helper;

        private Mock<ILogger> logger;

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ExceptionHandlingInConstructor()
        {
            this.cdpEvent2Helper = new CdpEvent2Helper(CreateMockConf(), null);
        }

        [TestMethod]
        public void GetDeleteReasonDefaultSuccess()
        {
            AccountCloseReason result = this.cdpEvent2Helper.GetDeleteReason(this.cdpEvent2);
            Assert.IsNotNull(result);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.logger = CreateMockGenevaLogger();
            this.cdpEvent2 = new CDPEvent2();
            this.cdpEvent2Helper = new CdpEvent2Helper(CreateMockConf(), this.logger.Object);
        }

        [TestMethod]
        public void TryGetCidInvalidDeleteDataSuccess()
        {
            //Arrange
            const long expectedCid = 1194684;
            var userDelete = new UserDelete
            {
                Property = new[] { InvalidDeleteData(1) }
            };
            this.cdpEvent2.EventData = userDelete;

            //Act
            var success = this.cdpEvent2Helper.TryGetCid(this.cdpEvent2, out long result);

            //Assert
            Assert.AreEqual(expectedCid, result);
            Assert.IsTrue(success);
        }

        [TestMethod]
        public void TryGetCidSuccess()
        {
            //Arrange
            const long expectedCid = 1194684;
            var userDelete = new UserDelete
            {
                Property = new[] { ValidDeleteData(1) }
            };
            this.cdpEvent2.EventData = userDelete;

            //Act
            var success = this.cdpEvent2Helper.TryGetCid(this.cdpEvent2, out long result);

            //Assert
            Assert.AreEqual(expectedCid, result);
            Assert.IsTrue(success);
        }

        [TestMethod]
        [DataRow(AccountCloseReason.UserAccountAgedOut, AccountCloseReason.UserAccountClosed)]
        [DataRow(AccountCloseReason.UserAccountCreationFailure, AccountCloseReason.UserAccountClosed)]
        [DataRow(AccountCloseReason.UserAccountClosed, AccountCloseReason.UserAccountAgedOut)]
        [DataRow(AccountCloseReason.None, AccountCloseReason.UserAccountCreationFailure)]
        public void TryGetDeleteReasonSuccess(AccountCloseReason reason, AccountCloseReason expected)
        {
            //Act
            var userDelete = new UserDelete
            {
                Property = new[] { ValidDeleteData((int)reason) }
            };
            this.cdpEvent2.EventData = userDelete;

            bool result = this.cdpEvent2Helper.TryGetDeleteReason(this.cdpEvent2, out AccountCloseReason actualReason);

            Assert.AreEqual(true, result);
            Assert.AreEqual((int)expected, (int)actualReason);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void TryGetGdprPreVerifierTokenFail(bool isEventDataNull)
        {
            if (!isEventDataNull)
            {
                //Arrange
                var deleteEvent = new UserDelete
                {
                    Property = new[]
                    {
                        new EventDataBaseProperty()
                    }
                };
                this.cdpEvent2.EventData = deleteEvent;
            }

            bool result = this.cdpEvent2Helper.TryGetGdprPreVerifierToken(this.cdpEvent2, out string token);
            Assert.IsNotNull(result);
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void TryGetGdprPreVerifierTokenSuccess()
        {
            //Arrange
            var userDelete = new UserDelete
            {
                Property = new[] { ValidDeleteData() }
            };
            this.cdpEvent2.EventData = userDelete;

            bool result = this.cdpEvent2Helper.TryGetGdprPreVerifierToken(this.cdpEvent2, out string token);
            Assert.IsNotNull(token);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TryGetLastSuccessLogin()
        {
            //Arrange
            var userDelete = new UserDelete
            {
                Property = new[] { ValidDeleteData(2) }
            };
            this.cdpEvent2.EventData = userDelete;

            bool result = this.cdpEvent2Helper.TryGetLastLogin(this.cdpEvent2, out DateTimeOffset time);
            Assert.IsTrue(result);
            Assert.AreNotEqual(default, time);
        }
    }
}
