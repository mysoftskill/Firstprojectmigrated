//--------------------------------------------------------------------------------
// <copyright file="AuditDataTest.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     Test AuditData class.
    /// </summary>
    [TestClass]
    public class AuditDataTest
    {
        [DataTestMethod]
        [DataRow(OperationResult.Success, "Successfully retrieved cert.")]
        [DataRow(OperationResult.Failure, "Unable to retrieve cert from key vault.")]
        [DataRow(OperationResult.Timeout, "Timeout when connecting to key vault.")]
        public void ValidateBuildAccessToKeyVaultOperationAuditData(OperationResult operationResult, string resultDescription)
        {
            string keyVaultURL = "https://foo.keyvault.com";
            string name = "fakeSecret";
            string callerDisplayName = "test caller";
            var expectedAuditEventCategory = new HashSet<AuditEventCategory>() { AuditEventCategory.KeyManagement };
            var callerIdentities = new List<CallerIdentity>
            {
                new CallerIdentity(CallerIdentityType.SubscriptionID, "subscription ID"),
                new CallerIdentity(CallerIdentityType.ObjectID, "object ID"),
            };

            var auditData = AuditData.BuildAccessToKeyVaultOperationAuditData(keyVaultURL, name, resultDescription, callerDisplayName, callerIdentities, operationResult);
            Assert.AreEqual(AuditEventType.Application, auditData.AuditEventType);
            Assert.AreEqual(AuditOperation.AccessToKeyVault, auditData.OperationName);
            Assert.AreEqual(operationResult, auditData.ResultType);
            Assert.AreEqual(resultDescription, auditData.ResultDescription);
            Assert.AreEqual(callerDisplayName, auditData.CallerDisplayName);
            Assert.IsTrue(auditData.AuditCategories.SetEquals(expectedAuditEventCategory));
            Assert.IsNotNull(auditData.CallerIdentities.Single(t => t.CallerIdentityType == CallerIdentityType.SubscriptionID.ToString() && t.CallerIdentityValue == "subscription ID"));
            Assert.IsNotNull(auditData.CallerIdentities.Single(t => t.CallerIdentityType == CallerIdentityType.ObjectID.ToString() && t.CallerIdentityValue == "object ID"));
            Assert.IsNotNull(auditData.TargetResources.Single(t => t.TargetResourceType == "KeyVaultBaseUrl" && t.TargetResourceName == keyVaultURL));
            Assert.IsNotNull(auditData.TargetResources.Single(t => t.TargetResourceType == "Name" && t.TargetResourceName == name));
        }

        [DataTestMethod]
        [DataRow(OperationResult.Success, "Successfully created data agent.")]
        [DataRow(OperationResult.Failure, "Failed to create data agent because of missing required data.")]
        public void ValidateCreateDataAgentOperationAuditData(OperationResult operationResult, string resultDescription)
        {
            var uri = new Uri("https://management.privacy.microsoft.com/api/v2/dataAgents");
            var id = Guid.NewGuid().ToString();
            var capabilities = new List<string> { "Delete", "Export" };
            var ownerId = Guid.NewGuid().ToString();

            string callerDisplayName = "test caller";
            var expectedAuditEventCategory = new HashSet<AuditEventCategory>() { AuditEventCategory.UserManagement };
            var callerIdentities = new List<CallerIdentity>
            {
                new CallerIdentity(CallerIdentityType.SubscriptionID, "subscription ID"),
                new CallerIdentity(CallerIdentityType.ObjectID, "object ID"),
            };

            var auditData = AuditData.BuildCreateDataAgentOperationAuditData(id, capabilities, ownerId, uri, resultDescription, callerDisplayName, callerIdentities, operationResult);
            Assert.AreEqual(AuditEventType.Management, auditData.AuditEventType);
            Assert.AreEqual(AuditOperation.CreateDataAgent, auditData.OperationName);
            Assert.AreEqual(operationResult, auditData.ResultType);
            Assert.AreEqual(resultDescription, auditData.ResultDescription);
            Assert.AreEqual(callerDisplayName, auditData.CallerDisplayName);
            Assert.IsTrue(auditData.AuditCategories.SetEquals(expectedAuditEventCategory));
            Assert.IsNotNull(auditData.CallerIdentities.Single(t => t.CallerIdentityType == CallerIdentityType.SubscriptionID.ToString() && t.CallerIdentityValue == "subscription ID"));
            Assert.IsNotNull(auditData.CallerIdentities.Single(t => t.CallerIdentityType == CallerIdentityType.ObjectID.ToString() && t.CallerIdentityValue == "object ID"));
            Assert.IsNotNull(auditData.TargetResources.Single(t => t.TargetResourceType == "RequestUri" && t.TargetResourceName == uri.ToString()));
            Assert.IsNotNull(auditData.ExtendedProperties);

            var extendedProperties = auditData.ExtendedProperties as DataAgentAudit;
            Assert.AreEqual(extendedProperties.Id, id);
            Assert.AreEqual(extendedProperties.OwnerId, ownerId);
            Assert.AreEqual(extendedProperties.Capabilities, string.Join(",", capabilities));
        }
    }
}
