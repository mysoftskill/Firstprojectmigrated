// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperienceContracts.UnitTests.PrivacySubject
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class AltSubjectTestCaseBase
    {
        protected void Assert_PrivacySubjectInvalidException_PropertyName(
            string expectedPropertyName,
            IPrivacySubject subject,
            SubjectUseContext useContext = SubjectUseContext.Any)
        {
            try
            {
                subject.Validate(useContext);
            }
            catch (PrivacySubjectInvalidException e)
            {
                Assert.AreEqual(expectedPropertyName, e.PropertyName);
                return;
            }
            catch
            {
                Assert.Fail("Unexpected exception had occurred.");
            }

            Assert.Fail("Exception was expected, but never occurred.");
        }
        protected void Assert_PrivacySubjectInvalidException_PropertyName(
            string expectedPropertyName,
            IPrivacySubject subject,
            Boolean isNewRule,
            SubjectUseContext useContext = SubjectUseContext.Any)
        {
            try
            {
                subject.Validate(useContext, isNewRule);
            }
            catch (PrivacySubjectInvalidException e)
            {
                Assert.AreEqual(expectedPropertyName, e.PropertyName);
                return;
            }
            catch
            {
                Assert.Fail("Unexpected exception had occurred.");
            }

            Assert.Fail("Exception was expected, but never occurred.");
        }
    }
}
