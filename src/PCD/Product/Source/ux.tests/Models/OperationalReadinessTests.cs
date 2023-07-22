using System;
using System.IO;
using Microsoft.PrivacyServices.UX.Models.Pdms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.PrivacyServices.UX.Tests.Models
{
    [TestClass]
    public class OperationalReadinessTests
    {
        private bool[] pdmsOpReadiness = new bool[128];
        private OperationalReadiness opReadiness;

        [TestInitialize]
        public void Initialize()
        {
            for(int i = 0; i < 128; i++)
            {
                // For simplicity and exhaustiveness, lets assign it like [T, F, T, F...]
                pdmsOpReadiness[i] = (i % 2 == 0) ? true : false;
            }

            opReadiness = new OperationalReadiness(pdmsOpReadiness);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_Throws_DataMisalignedException()
        {
            new OperationalReadiness(new bool[] { true, false });
        }

        [TestMethod]
        public void Ctor_Initializes_Properties_Properly()
        {
            var errorMsg = "Data contract broken, please do not change this test. Read code comment on top of OperationalReadiness.cs.";

            Assert.IsTrue(opReadiness.IsLoggingEnabled, errorMsg);
            Assert.IsFalse(opReadiness.IsLoggingCompliant, errorMsg);

            Assert.IsTrue(opReadiness.IsLoggingIncludesCommandId, errorMsg);
            Assert.IsFalse(opReadiness.IsReliabilityAlertsTrigger, errorMsg);

            Assert.IsTrue(opReadiness.IsLatencyAlertsTrigger, errorMsg);
            Assert.IsFalse(opReadiness.IsMonitoringSla, errorMsg);

            Assert.IsTrue(opReadiness.IsScalableForCommandRate, errorMsg);
            Assert.IsFalse(opReadiness.IsAlertsInIcm, errorMsg);

            Assert.IsTrue(opReadiness.IsDriDocumentation, errorMsg);
            Assert.IsFalse(opReadiness.IsDriEscalation, errorMsg);

            Assert.IsTrue(opReadiness.IsIncidentSeverityDoc, errorMsg);
            Assert.IsFalse(opReadiness.IsGuidesPublished, errorMsg);

            Assert.IsTrue(opReadiness.IsE2eValidation, errorMsg);
            Assert.IsFalse(opReadiness.IsCertExpiryAlerts, errorMsg);

            Assert.IsTrue(opReadiness.IsCertChangeDoc, errorMsg);
            Assert.IsFalse(opReadiness.IsServiceRecoveryPlan, errorMsg);

            Assert.IsTrue(opReadiness.IsServiceInProd, errorMsg);
            Assert.IsFalse(opReadiness.IsDisasterRecoveryPlan, errorMsg);

            Assert.IsTrue(opReadiness.IsDisasterRecoveryTested, errorMsg);
        }

        [TestMethod]
        public void Default_Ctor_Initializes_Properties_To_False()
        {
            opReadiness = new OperationalReadiness();

            Assert.IsFalse(opReadiness.IsLoggingEnabled);
            Assert.IsFalse(opReadiness.IsLoggingCompliant);
            Assert.IsFalse(opReadiness.IsLoggingIncludesCommandId);
            Assert.IsFalse(opReadiness.IsReliabilityAlertsTrigger);
            Assert.IsFalse(opReadiness.IsLatencyAlertsTrigger);
            Assert.IsFalse(opReadiness.IsMonitoringSla);
            Assert.IsFalse(opReadiness.IsScalableForCommandRate);
            Assert.IsFalse(opReadiness.IsAlertsInIcm);
            Assert.IsFalse(opReadiness.IsDriDocumentation);
            Assert.IsFalse(opReadiness.IsDriEscalation);
            Assert.IsFalse(opReadiness.IsIncidentSeverityDoc);
            Assert.IsFalse(opReadiness.IsGuidesPublished);
            Assert.IsFalse(opReadiness.IsE2eValidation);
            Assert.IsFalse(opReadiness.IsCertExpiryAlerts);
            Assert.IsFalse(opReadiness.IsCertChangeDoc);
            Assert.IsFalse(opReadiness.IsServiceRecoveryPlan);
            Assert.IsFalse(opReadiness.IsServiceInProd);
            Assert.IsFalse(opReadiness.IsDisasterRecoveryPlan);
            Assert.IsFalse(opReadiness.IsDisasterRecoveryTested);
        }

        [TestMethod]
        public void Gets_PdmsOpReadiness_Properly()
        {
            var errorMsg = "Data contract broken, please do not change this test. Read code comment on top of OperationalReadiness.cs.";
            CollectionAssert.AreEqual(opReadiness.GetPdmsOpReadinessChecklist(), pdmsOpReadiness, errorMsg);
        }

        [TestMethod]
        public void Serialization_Deserialization_Validation()
        {
            //  Serialization
            var serialized = JsonConvert.SerializeObject(opReadiness);
            Assert.IsNotNull(serialized);

            //  Deserialization
            OperationalReadiness deserializedOpReadiness = JsonConvert.DeserializeObject<OperationalReadiness>(serialized);

            Assert.IsTrue(deserializedOpReadiness.IsLoggingEnabled);
            Assert.IsFalse(deserializedOpReadiness.IsLoggingCompliant);

            Assert.IsTrue(deserializedOpReadiness.IsLoggingIncludesCommandId);
            Assert.IsFalse(deserializedOpReadiness.IsReliabilityAlertsTrigger);

            Assert.IsTrue(deserializedOpReadiness.IsLatencyAlertsTrigger);
            Assert.IsFalse(deserializedOpReadiness.IsMonitoringSla);

            Assert.IsTrue(deserializedOpReadiness.IsScalableForCommandRate);
            Assert.IsFalse(deserializedOpReadiness.IsAlertsInIcm);

            Assert.IsTrue(deserializedOpReadiness.IsDriDocumentation);
            Assert.IsFalse(deserializedOpReadiness.IsDriEscalation);

            Assert.IsTrue(deserializedOpReadiness.IsIncidentSeverityDoc);
            Assert.IsFalse(deserializedOpReadiness.IsGuidesPublished);

            Assert.IsTrue(deserializedOpReadiness.IsE2eValidation);
            Assert.IsFalse(deserializedOpReadiness.IsCertExpiryAlerts);

            Assert.IsTrue(deserializedOpReadiness.IsCertChangeDoc);
            Assert.IsFalse(deserializedOpReadiness.IsServiceRecoveryPlan);

            Assert.IsTrue(deserializedOpReadiness.IsServiceInProd);
            Assert.IsFalse(deserializedOpReadiness.IsDisasterRecoveryPlan);

            Assert.IsTrue(deserializedOpReadiness.IsDisasterRecoveryTested);
        }
    }
}
