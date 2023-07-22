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
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    [TestClass]
    public class MsaSelfAuthSubjectTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MsaSelfAuthSubject_Throws_UserProxyTicket_Null()
        {
            new MsaSelfAuthSubject(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void MsaSelfAuthSubject_Throws_UserProxyTicket_Empty()
        {
            new MsaSelfAuthSubject(string.Empty);
        }

        [TestMethod]
        public void MsaSelfAuthSubject_GetUserProxyTicket_ReturnsOriginalValue()
        {
            var msaSelfAuthSubject = new MsaSelfAuthSubject("proxy ticket");
            Assert.AreEqual("proxy ticket", msaSelfAuthSubject.GetUserProxyTicket());
        }

        [TestMethod]
        public void MsaSelfAuthSubject_Serialization_DoesNotSerializeUserProxyTicket()
        {
            var msaSelfAuthSubjectJson = JsonConvert.SerializeObject(new MsaSelfAuthSubject("proxy ticket"));

            var msaSelfAuthSubject = JsonConvert.DeserializeObject<MsaSelfAuthSubject>(msaSelfAuthSubjectJson);
            Assert.IsNull(msaSelfAuthSubject.GetUserProxyTicket(), $"{nameof(MsaSelfAuthSubject)} is not allowed to serialize user proxy ticket.");
        }
    }
}
