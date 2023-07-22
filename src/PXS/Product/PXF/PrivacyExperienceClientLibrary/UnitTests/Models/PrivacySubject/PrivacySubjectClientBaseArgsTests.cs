// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.UnitTests.Models.PrivacySubject
{
    using System;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models.PrivacySubject;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class PrivacySubjectClientBaseArgsTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void PrivacySubjectClientBaseArgs_CtorThrows_SubjectIsNull()
        {
            new TestPrivacySubjectClientBaseArgs(null);
        }

        [TestMethod]
        public void PrivacySubjectClientBaseArgs_Ctor_AssignsSubject_NoUserProxyTicket()
        {
            var privacySubjectMock = this.CreateMockPrivacySubject();

            var args = new TestPrivacySubjectClientBaseArgs(privacySubjectMock.Object);
            Assert.AreSame(privacySubjectMock.Object, args.Subject);
            Assert.IsNull(args.UserProxyTicket, "All but one privacy subjects should not result in setting user proxy ticket.");
        }

        [TestMethod]
        public void PrivacySubjectClientBaseArgs_Ctor_AssignsUserProxyTicket_MsaSelfAuthSubject()
        {
            var privacySubject = new MsaSelfAuthSubject("user proxy ticket");

            var args = new TestPrivacySubjectClientBaseArgs(privacySubject);
            Assert.AreSame(privacySubject, args.Subject);
            Assert.AreEqual("user proxy ticket", args.UserProxyTicket, $"{nameof(MsaSelfAuthSubject)} is expected to provide user proxy ticket for privacy subject operations.");
        }

        private Mock<IPrivacySubject> CreateMockPrivacySubject()
        {
            var privacySubjectMock = new Mock<IPrivacySubject>(MockBehavior.Strict);

            return privacySubjectMock;
        }

        class TestPrivacySubjectClientBaseArgs : PrivacySubjectClientBaseArgs
        {
            public TestPrivacySubjectClientBaseArgs(IPrivacySubject subject)
                : base(subject)
            {
            }
        }
    }
}
