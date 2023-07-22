using System;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Operational readiness checklist state.
    /// 
    /// NOTE: Change management on this file should happen with extreme caution. Please map your 
    /// scenario below and make code changes accordingly. 
    /// 
    /// Scenario 1: Adding a new question on UI.
    /// Add a new property in this class and in tests.
    /// 
    /// Scenario 2: Removing a question from UI.
    /// Remove the property from this class and in tests.
    /// Do NOT change the index values. This will break storage contract.
    /// 
    /// Scenario 3: Replacing a question in UI.
    /// Add a new property for the replaced question and in tests.
    /// Do NOT replace the name of the property. This will break storage contract.
    /// 
    /// Scenario 4: Changing the wording on an exisiting question in UI.
    /// No change required here, just UI. 
    /// 
    /// Scenario 5: Changing the relative ordering of questions on the UI.
    /// No change required here, just UI. 
    /// </summary>
    public class OperationalReadiness
    {
        private const int IndexSize = 128;

        //  Initializing with IndexSize locks down the size of this array.
        private readonly bool[] opReadinessChecklist = new bool[IndexSize];

        public OperationalReadiness()
        {
        }

        public OperationalReadiness(bool[] pdmsOpReadinessChecklist)
        {
            if (pdmsOpReadinessChecklist.Length != IndexSize)
            {
                throw new ArgumentException("PDMS returned an array of unexpected size. This will cause data misalignment.");
            }

            opReadinessChecklist = pdmsOpReadinessChecklist;
        }

        public bool IsLoggingEnabled
        {
            get => opReadinessChecklist[0];
            set => opReadinessChecklist[0] = value;
        }

        public bool IsLoggingCompliant
        {
            get => opReadinessChecklist[1];
            set => opReadinessChecklist[1] = value;
        }

        public bool IsLoggingIncludesCommandId
        {
            get => opReadinessChecklist[2];
            set => opReadinessChecklist[2] = value;
        }

        public bool IsReliabilityAlertsTrigger
        {
            get => opReadinessChecklist[3];
            set => opReadinessChecklist[3] = value;
        }

        public bool IsLatencyAlertsTrigger
        {
            get => opReadinessChecklist[4];
            set => opReadinessChecklist[4] = value;
        }

        public bool IsMonitoringSla
        {
            get => opReadinessChecklist[5];
            set => opReadinessChecklist[5] = value;
        }

        public bool IsScalableForCommandRate
        {
            get => opReadinessChecklist[6];
            set => opReadinessChecklist[6] = value;
        }

        public bool IsAlertsInIcm
        {
            get => opReadinessChecklist[7];
            set => opReadinessChecklist[7] = value;
        }

        public bool IsDriDocumentation
        {
            get => opReadinessChecklist[8];
            set => opReadinessChecklist[8] = value;
        }

        public bool IsDriEscalation
        {
            get => opReadinessChecklist[9];
            set => opReadinessChecklist[9] = value;
        }

        public bool IsIncidentSeverityDoc
        {
            get => opReadinessChecklist[10];
            set => opReadinessChecklist[10] = value;
        }

        public bool IsGuidesPublished
        {
            get => opReadinessChecklist[11];
            set => opReadinessChecklist[11] = value;
        }

        public bool IsE2eValidation
        {
            get => opReadinessChecklist[12];
            set => opReadinessChecklist[12] = value;
        }

        public bool IsCertExpiryAlerts
        {
            get => opReadinessChecklist[13];
            set => opReadinessChecklist[13] = value;
        }

        public bool IsCertChangeDoc
        {
            get => opReadinessChecklist[14];
            set => opReadinessChecklist[14] = value;
        }

        public bool IsServiceRecoveryPlan
        {
            get => opReadinessChecklist[15];
            set => opReadinessChecklist[15] = value;
        }

        public bool IsServiceInProd
        {
            get => opReadinessChecklist[16];
            set => opReadinessChecklist[16] = value;
        }

        public bool IsDisasterRecoveryPlan
        {
            get => opReadinessChecklist[17];
            set => opReadinessChecklist[17] = value;
        }

        public bool IsDisasterRecoveryTested
        {
            get => opReadinessChecklist[18];
            set => opReadinessChecklist[18] = value;
        }

        public bool[] GetPdmsOpReadinessChecklist()
        {
            bool[] checklistItems = new bool[]
            {
                IsLoggingEnabled,
                IsLoggingCompliant,
                IsLoggingIncludesCommandId,
                IsReliabilityAlertsTrigger,
                IsLatencyAlertsTrigger,
                IsMonitoringSla,
                IsScalableForCommandRate,
                IsAlertsInIcm,
                IsDriDocumentation,
                IsDriEscalation,
                IsIncidentSeverityDoc,
                IsGuidesPublished,
                IsE2eValidation,
                IsCertExpiryAlerts,
                IsCertChangeDoc,
                IsServiceRecoveryPlan,
                IsServiceInProd,
                IsDisasterRecoveryPlan,
                IsDisasterRecoveryTested
            };

            Array.Clear(opReadinessChecklist, 0, IndexSize);

            //  Fill all checklist item values.
            for (int i = 0; i < checklistItems.Length; i++)
            {
                opReadinessChecklist[i] = checklistItems[i];
            }

            return opReadinessChecklist;
        }
    }
}
