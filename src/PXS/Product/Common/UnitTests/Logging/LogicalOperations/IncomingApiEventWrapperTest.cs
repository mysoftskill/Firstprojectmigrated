// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.UnitTests.LogicalOperations
{
    using System;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ms.Qos;

    [TestClass]
    public class IncomingApiEventWrapperTest
    {
        private const string ServiceLevelErrorStatusCode = "500";
        private const string ClientLevelErrorStatusCode = "400";
        private const string SuccesStatusCode = "200";

        [TestInitialize]
        public void TestInitialize()
        {
            Sll.ResetContext();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }

        #region Start() tests

        [TestMethod]
        public void IncomingApiEventWrapperStartSuccess()
        {
            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper();
            apiEvent.Start(FrontEndApiNames.SwitchPaymentInstruments);
        }

        [ExpectedException(typeof(InvalidOperationException))]
        [TestMethod]
        public void IncomingApiEventWrapperStartTwice()
        {
            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper();
            apiEvent.Start(FrontEndApiNames.GetSubscriptions);
            apiEvent.Start(FrontEndApiNames.GetSubscriptions);
        }

        #endregion

        #region Finish() tests

        [TestMethod]
        public void IncomingApiEventWrapperFinishSuccessfulOperation()
        {
            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper();

            apiEvent.ProtocolStatusCode = SuccesStatusCode;
            apiEvent.Start(FrontEndApiNames.GetServiceDetailOffice);
            apiEvent.Finish(true);

            Assert.AreEqual(ServiceRequestStatus.Success, apiEvent.RequestStatus);
        }

        [TestMethod]
        public void IncomingApiEventWrapperFinishSuccessfulOperationServiceLevelErrorCode()
        {
            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper();

            apiEvent.ProtocolStatusCode = ServiceLevelErrorStatusCode;
            apiEvent.Start(FrontEndApiNames.GetServiceDetailOffice);
            apiEvent.Finish(true);

            Assert.AreEqual(ServiceRequestStatus.Success, apiEvent.RequestStatus, "Despite a 5xx level error code, if success was indicated it should be honored");
        }

        [TestMethod]
        public void IncomingApiEventWrapperFinishSuccessfulOperationClientLevelErrorCode()
        {
            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper();

            apiEvent.ProtocolStatusCode = ClientLevelErrorStatusCode;
            apiEvent.Start(FrontEndApiNames.GetServiceDetailOffice);
            apiEvent.Finish(true);

            Assert.AreEqual(ServiceRequestStatus.Success, apiEvent.RequestStatus, "Despite a 4xx level error code, if success was indicated it should be honored");
        }

        [TestMethod]
        public void IncomingApiEventWrapperFinishSuccessfulOperationQosImpactingOverride()
        {
            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper();

            apiEvent.ProtocolStatusCode = ServiceLevelErrorStatusCode;
            apiEvent.Start(FrontEndApiNames.GetServiceDetailOffice);
            apiEvent.Finish(overrideQosImpacting: true);

            Assert.AreEqual(ServiceRequestStatus.Success, apiEvent.RequestStatus, "Despite the override, a succeful operation should return Success");
        }

        [TestMethod]
        public void IncomingApiEventWrapperFinishFailedOperationClientError()
        {
            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper();

            apiEvent.ProtocolStatusCode = ClientLevelErrorStatusCode;
            apiEvent.Start(FrontEndApiNames.GetServiceDetailOffice);
            apiEvent.Success = false;
            apiEvent.Finish();

            Assert.AreEqual(ServiceRequestStatus.CallerError, apiEvent.RequestStatus);
        }

        [TestMethod]
        public void IncomingApiEventWrapperFinishFailedOperationServiceError()
        {
            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper();

            apiEvent.ProtocolStatusCode = ServiceLevelErrorStatusCode;
            apiEvent.Start(FrontEndApiNames.GetServiceDetailOffice);
            apiEvent.Success = false;
            apiEvent.Finish();

            Assert.AreEqual(ServiceRequestStatus.ServiceError, apiEvent.RequestStatus);
        }

        [TestMethod]
        public void IncomingApiEventWrapperFinishFailedOperationSuccessStatusCode()
        {
            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper();

            apiEvent.ProtocolStatusCode = SuccesStatusCode;
            apiEvent.Start(FrontEndApiNames.GetServiceDetailOffice);
            apiEvent.Success = false;
            apiEvent.Finish();

            Assert.AreEqual(ServiceRequestStatus.ServiceError, apiEvent.RequestStatus);
        }

        [TestMethod]
        public void IncomingApiEventWrapperFinishFailedOperationClientErrorOverride()
        {
            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper();

            apiEvent.ProtocolStatusCode = ClientLevelErrorStatusCode;
            apiEvent.Start(FrontEndApiNames.GetServiceDetailOffice);
            apiEvent.Success = false;
            apiEvent.Finish(overrideQosImpacting: true);

            Assert.AreEqual(ServiceRequestStatus.CallerError, apiEvent.RequestStatus);
        }

        [TestMethod]
        public void IncomingApiEventWrapperFinishFailedOperationServiceErrorOverride()
        {
            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper();

            apiEvent.ProtocolStatusCode = ServiceLevelErrorStatusCode;
            apiEvent.Start(FrontEndApiNames.GetServiceDetailOffice);
            apiEvent.Success = false;
            apiEvent.Finish(overrideQosImpacting: true);

            Assert.AreEqual(ServiceRequestStatus.CallerError, apiEvent.RequestStatus);
        }

        [TestMethod]
        public void IncomingApiEventWrapperFinishFailedOperationSuccessStatusCodeOverride()
        {
            IncomingApiEventWrapper apiEvent = new IncomingApiEventWrapper();

            apiEvent.ProtocolStatusCode = SuccesStatusCode;
            apiEvent.Start(FrontEndApiNames.GetServiceDetailOffice);
            apiEvent.Success = false;
            apiEvent.Finish(overrideQosImpacting: true);

            Assert.AreEqual(ServiceRequestStatus.CallerError, apiEvent.RequestStatus);
        }

        #endregion
    }
}
