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
    public class OperationRequestBaseTests
    {
        [TestMethod]
        public void OperationRequestBase_Subject_Serialization_MsaSelfAuthSubject()
        {
            var operationRequestJson = JsonConvert.SerializeObject(new TestOperationRequestBase
            {
                Subject = new MsaSelfAuthSubject("proxy ticket")
            });

            var operationRequest = JsonConvert.DeserializeObject<TestOperationRequestBase>(operationRequestJson);
            Assert.IsInstanceOfType(operationRequest.Subject, typeof(MsaSelfAuthSubject));
        }

        [TestMethod]
        public void OperationRequestBase_Subject_Serialization_DemographicSubject()
        {
            var operationRequestJson = JsonConvert.SerializeObject(new TestOperationRequestBase
            {
                Subject = new DemographicSubject()
            });

            var operationRequest = JsonConvert.DeserializeObject<TestOperationRequestBase>(operationRequestJson);
            Assert.IsInstanceOfType(operationRequest.Subject, typeof(DemographicSubject));
        }

        [TestMethod]
        public void OperationRequestBase_Subject_Serialization_MicrosoftEmployeeSubject()
        {
            var operationRequestJson = JsonConvert.SerializeObject(new TestOperationRequestBase
            {
                Subject = new MicrosoftEmployeeSubject()
            });

            var operationRequest = JsonConvert.DeserializeObject<TestOperationRequestBase>(operationRequestJson);
            Assert.IsInstanceOfType(operationRequest.Subject, typeof(MicrosoftEmployeeSubject));
        }

        class TestOperationRequestBase : OperationRequestBase
        {
        }
    }
}
