// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.MsaAccountClose
{
    using System;

    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AccountDeleteInformationTests
    {
        [DataTestMethod]
        [DataRow(AccountCloseReason.None, typeof(AccountCloseRequest))]
        [DataRow(AccountCloseReason.Test, typeof(AccountCloseRequest))]
        [DataRow(AccountCloseReason.UserAccountClosed, typeof(AccountCloseRequest))]
        [DataRow(AccountCloseReason.UserAccountCreationFailure, typeof(AccountCloseRequest))]
        [DataRow(AccountCloseReason.UserAccountAgedOut, typeof(AgeOutRequest))]
        public void ShouldGenerateExpectedTypeBasedOnCloseReason(AccountCloseReason reason, Type expected)
        {
            var info = new AccountDeleteInformation
            {
                Reason = reason
            };

            var request = info.ToAccountCloseRequest("test");
            var actual = request.GetType();
            Assert.AreEqual(expected, actual);

            Assert.IsTrue(request.ProcessorApplicable);
            Assert.IsTrue(request.ControllerApplicable);
        }
    }
}