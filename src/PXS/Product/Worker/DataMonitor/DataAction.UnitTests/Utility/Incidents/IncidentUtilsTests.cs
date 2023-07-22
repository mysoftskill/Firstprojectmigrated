// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Utility.Incidents
{
    using Microsoft.AzureAd.Icm.Types;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Incidents;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Incident = Microsoft.PrivacyServices.DataManagement.Client.V2.Incident;

    [TestClass]
    public class IncidentUtilsTests
    {
        [TestMethod]
        public void ToIncidentFileStatusReturnsFailedWhenNullIncident()
        {
            IncidentFileStatus result;

            result = IncidentUtils.ToIncidentFileStatus(null);

            Assert.AreEqual(IncidentFileStatus.FailedToFile, result);
        }

        [TestMethod]
        public void ToIncidentFileStatusReturnsFailedWhenResultMetadataIsNull()
        {
            IncidentFileStatus result;

            result = IncidentUtils.ToIncidentFileStatus(new Incident());

            Assert.AreEqual(IncidentFileStatus.Created, result);
        }

        [TestMethod]
        [DataRow(IncidentAddUpdateStatus.AddedNew, IncidentAddUpdateSubStatus.None, IncidentFileStatus.Created)]
        [DataRow(IncidentAddUpdateStatus.AddedNew, IncidentAddUpdateSubStatus.Suppressed, IncidentFileStatus.CreatedSuppressed)]
        [DataRow(IncidentAddUpdateStatus.Discarded, IncidentAddUpdateSubStatus.None, IncidentFileStatus.HitCounted)]
        [DataRow(IncidentAddUpdateStatus.Discarded, IncidentAddUpdateSubStatus.Suppressed, IncidentFileStatus.Discarded)]
        [DataRow(IncidentAddUpdateStatus.UpdatedExisting, IncidentAddUpdateSubStatus.None, IncidentFileStatus.Updated)]
        [DataRow(IncidentAddUpdateStatus.UpdatedExisting, IncidentAddUpdateSubStatus.Activated, IncidentFileStatus.UpdatedActivate)]
        [DataRow(
            IncidentAddUpdateStatus.UpdatedExisting, 
            IncidentAddUpdateSubStatus.Suppressed, 
            IncidentFileStatus.UpdatedSuppressed)]
        [DataRow(IncidentAddUpdateStatus.DidNotChangeExisting, IncidentAddUpdateSubStatus.None, IncidentFileStatus.Updated)]
        [DataRow(
            IncidentAddUpdateStatus.DidNotChangeExisting, 
            IncidentAddUpdateSubStatus.Suppressed, 
            IncidentFileStatus.UpdatedSuppressed)]
        [DataRow(IncidentAddUpdateStatus.Invalid, IncidentAddUpdateSubStatus.None, IncidentFileStatus.FailedToFile)]
        [DataRow(IncidentAddUpdateStatus.UpdateToHoldingNotAllowed, IncidentAddUpdateSubStatus.None, IncidentFileStatus.FailedToFile)]
        [DataRow(IncidentAddUpdateStatus.AlertSourceUpdatesPending, IncidentAddUpdateSubStatus.None, IncidentFileStatus.FailedToFile)]
        public void ToIncidentFileStatusReturnsFailedWhenStatusCreatedAndSubStatusEmpty(
            IncidentAddUpdateStatus status,
            IncidentAddUpdateSubStatus subStatus,
            IncidentFileStatus expectged)
        {
            IncidentFileStatus result;

            result = IncidentUtils.ToIncidentFileStatus(
                new Incident
                {
                    ResponseMetadata = new IncidentResponseMetadata { Status = (int)status, Substatus = (int)subStatus }
                });

            Assert.AreEqual(expectged, result);
        }
    }
}
