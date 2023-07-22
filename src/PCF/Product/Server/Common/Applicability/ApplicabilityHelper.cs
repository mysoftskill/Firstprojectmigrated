namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.PrivacyServices.SignalApplicability;

    /// <summary>
    /// Defines the <see cref="ApplicabilityHelper" />
    /// </summary>
    public static class ApplicabilityHelper
    {
        /// <summary>
        /// Check if command is blocked in PCF.
        /// </summary>
        /// <param name="command">The command<see cref="PrivacyCommand"/></param>
        /// <returns>The <see cref="ApplicabilityResult"/></returns>
        public static ApplicabilityResult CheckIfBlockedInPcf(PrivacyCommand command)
        {
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();

            if (command.IsSyntheticTestCommand && !Config.Instance.Frontdoor.IsSyntheticTestAgent(command.AgentId))
            {
                // If it is a synthetic test command, but not destined for a synthetic test agent, it's not applicable.
                applicabilityResult.ReasonCode = ApplicabilityReasonCode.SyntheticTestCommand;
                applicabilityResult.Status = ApplicabilityStatus.DoesNotApply;
                return applicabilityResult;
            }

            if (FlightingUtilities.IsAgentBlocked(command.AgentId))
            {
                applicabilityResult.ReasonCode = ApplicabilityReasonCode.AgentIsBlocked;
                applicabilityResult.Status = ApplicabilityStatus.DoesNotApply;
                return applicabilityResult;
            }

            if (FlightingUtilities.IsAssetGroupIdBlocked(command.AssetGroupId))
            {
                applicabilityResult.ReasonCode = ApplicabilityReasonCode.AssetGroupIsBlocked;
                applicabilityResult.Status = ApplicabilityStatus.DoesNotApply;
                return applicabilityResult;
            }

            return applicabilityResult;
        }

        /// <summary>
        /// Check agent readiness.
        /// </summary>
        /// <param name="assetGroupInfo">The assetGroupInfo<see cref="IAssetGroupInfo"/></param>
        /// <param name="command">The command<see cref="PrivacyCommand"/></param>
        /// <returns>The <see cref="ApplicabilityResult"/></returns>
        public static ApplicabilityResult CheckAgentReadiness(IAssetGroupInfo assetGroupInfo, PrivacyCommand command)
        {
            ApplicabilityResult applicabilityResult = new ApplicabilityResult();
            IDataAgentInfo agentInfo = assetGroupInfo.AgentInfo;

            bool isExportTestInProduction = assetGroupInfo.AgentReadinessState == AgentReadinessState.TestInProd && command.CommandType == PrivacyCommandType.Export;

            if (assetGroupInfo.AgentReadinessState == AgentReadinessState.TestInProd && !agentInfo.IsOnline)
            {
                applicabilityResult.ReasonCode = ApplicabilityReasonCode.TipAgentIsNotOnline;
                applicabilityResult.Status = ApplicabilityStatus.DoesNotApply;
                return applicabilityResult;
            }

            if (isExportTestInProduction)
            {
                if (command.Subject is AadSubject exportAadSubject)
                {
                    if (!FlightingUtilities.IsTenantIdEnabled(FlightingNames.TestInProductionByTenantIdEnabled, new TenantId(exportAadSubject.TenantId)))
                    {
                        applicabilityResult.ReasonCode = ApplicabilityReasonCode.TipAgentNotInTestTenantIdFlight;
                        applicabilityResult.Status = ApplicabilityStatus.DoesNotApply;
                        return applicabilityResult;
                    }
                }
                else if (command.CommandSource != Portals.PartnerTestPage)
                {
                    applicabilityResult.ReasonCode = ApplicabilityReasonCode.TipAgentShouldNotReceiveProdCommands;
                    applicabilityResult.Status = ApplicabilityStatus.DoesNotApply;
                    return applicabilityResult;
                }
            }

            return applicabilityResult;
        }

        /// <summary>
        /// Is the given applicability reason code dependent on the data tags 
        /// and hence affected by temporary datagrid and datamap issues
        /// If it is tag dependent, dont complete the command for the associated asset group
        /// during GetCommands and QueryCommands. 
        /// </summary>
        /// <param name="applicabilityReasonCode">the applicability reason code being verified</param>
        /// <returns>true if the reason code is one of the specified ones else false</returns>
        public static bool IsApplicabilityResultTagDependent(ApplicabilityReasonCode applicabilityReasonCode)
        {
            switch (applicabilityReasonCode)
            {
               case ApplicabilityReasonCode.AssetGroupInfoIsDeprecated:
                case ApplicabilityReasonCode.AssetGroupInfoIsInvalid:
                case ApplicabilityReasonCode.AssetGroupNotOptInToMsaAgeOut:
                case ApplicabilityReasonCode.AssetGroupStartTimeLaterThanRequestTimeStamp:
                case ApplicabilityReasonCode.DoesNotMatchAadSubjectTenantId:
                case ApplicabilityReasonCode.DoesNotMatchAssetGroupCapability:
                case ApplicabilityReasonCode.DoesNotMatchAssetGroupDataTypes:
                case ApplicabilityReasonCode.DoesNotMatchAssetGroupSubjects:
                case ApplicabilityReasonCode.DoesNotMatchAssetGroupSupportedCloudInstances:
                case ApplicabilityReasonCode.IgnoreAccountCloseForEmployeeControllerDataTypes:
                    return true;

                default:
                    return false;
            }
        }
    }
}