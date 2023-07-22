// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using ClientLibrary;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public sealed class ExpectedPrivacyExperienceTransportException : ExpectedExceptionBaseAttribute
    {
        private string _expectedExceptionMessage;

        public ExpectedPrivacyExperienceTransportException(string expectedExceptionMessage)
        {
            _expectedExceptionMessage = expectedExceptionMessage;
        }

        protected override void Verify(Exception exception)
        {
            PrivacyExperienceTransportException privacyExperienceTransportException = exception as PrivacyExperienceTransportException;
            Assert.IsNotNull(exception);

            Assert.IsInstanceOfType(exception, typeof(PrivacyExperienceTransportException), "Wrong type of exception was thrown.");

            if (!_expectedExceptionMessage.Length.Equals(0))
            {
                Assert.AreEqual(_expectedExceptionMessage, privacyExperienceTransportException.Error.Message, "Wrong exception message was returned.");
            }
        }
    }

}
