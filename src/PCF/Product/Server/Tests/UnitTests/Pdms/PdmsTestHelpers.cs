namespace PCF.UnitTests.Pdms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.Policy;

    using Client = Microsoft.PrivacyServices.CommandFeed.Client;

    public static class PdmsTestHelpers
    {
        public static void SetValueToAssetGroupInfo(
            AssetGroupInfoDocument asssGroupInfo,
            string assetGroupQualifier,
            string[] capabilities,
            string[] subjectTypes,
            string[] dataTypes = null)
        {
            asssGroupInfo.AssetGroupId = new AssetGroupId(Guid.NewGuid());
            asssGroupInfo.AssetGroupQualifier = assetGroupQualifier;
            asssGroupInfo.Capabilities = capabilities;
            asssGroupInfo.SubjectTypes = subjectTypes;
            asssGroupInfo.DataTypes = dataTypes;
            asssGroupInfo.VariantInfosAppliedByAgents = new List<AssetGroupVariantInfoDocument>();
            asssGroupInfo.VariantInfosAppliedByPcf = new List<AssetGroupVariantInfoDocument>();
        }

        public static PrivacyCommand CreatePrivacyCommand(
            PcfTestCapability capability, 
            PdmsSubjectType pcfTestSubjectType, 
            PcfTestDataType[] dataTypeIds, 
            IAssetGroupInfo assetGroupInfo,
            string tenantid)
        {
            PrivacyCommand privacyCommand;
            AgentId agentId = new AgentId(Guid.NewGuid());

            if (capability == PcfTestCapability.Delete && pcfTestSubjectType == PdmsSubjectType.AADUser)
            {
                throw new ArgumentException("AAD subjects will never receive “Delete” commands, only “AccountClose” and “Export” - Hans(c)");
            }

            if (capability == PcfTestCapability.Delete)
            {
                privacyCommand = new DeleteCommand(
                        agentId: agentId,
                        assetGroupId: assetGroupInfo.AssetGroupId,
                        assetGroupQualifier: assetGroupInfo.AssetGroupQualifier,
                        verifier: string.Empty,
                        verifierV3: string.Empty,
                        batchId: new RequestBatchId(Guid.NewGuid()),
                        clientCommandState: "Test Please Ignore",
                        commandId: new CommandId(Guid.NewGuid().ToString()),
                        dataType: dataTypeIds.First().GetDataTypeId(),
                        dataTypePredicate: null,
                        timePredicate: new TimeRangePredicate
                        {
                            StartTime = DateTime.UtcNow.AddDays(30),
                            EndTime = DateTimeOffset.UtcNow,
                        },
                        nextVisibleTime: DateTimeOffset.FromUnixTimeSeconds(100).ToNearestMsUtc(),
                        subject: pcfTestSubjectType.GetPrivacySubject(),
                        correlationVector: "Test Please Ignore",
                        timestamp: DateTimeOffset.FromUnixTimeSeconds(100).ToNearestMsUtc(),
                        cloudInstance: Policies.Current.CloudInstances.Ids.Public.Value,
                        commandSource: string.Empty,
                        processorApplicable: true,
                        controllerApplicable: true,
                        absoluteExpirationTime: DateTimeOffset.UtcNow.AddDays(1),
                        queueStorageType: QueueStorageType.AzureCosmosDb);
            }
            else if (capability == PcfTestCapability.Export)
            {
                privacyCommand = new ExportCommand(
                    agentId: agentId,
                    assetGroupId: assetGroupInfo.AssetGroupId,
                    assetGroupQualifier: assetGroupInfo.AssetGroupQualifier,
                    verifier: string.Empty,
                    verifierV3: string.Empty,
                    batchId: new RequestBatchId(Guid.NewGuid()),
                    clientCommandState: "Test Please Ignore",
                    commandId: new CommandId(Guid.NewGuid()),
                    dataTypes: dataTypeIds.Select(o => o.GetDataTypeId()).ToArray(),
                    nextVisibleTime: DateTimeOffset.FromUnixTimeSeconds(100).ToNearestMsUtc(),
                    subject: pcfTestSubjectType.GetPrivacySubject(),
                    correlationVector: "Test Please Ignore",
                    timestamp: DateTimeOffset.FromUnixTimeSeconds(100).ToNearestMsUtc(),
                    cloudInstance: Policies.Current.CloudInstances.Ids.Public.Value,
                    commandSource: string.Empty,
                    processorApplicable: true,
                    controllerApplicable: true,
                    absoluteExpirationTime: DateTimeOffset.UtcNow.AddDays(1),
                    queueStorageType: QueueStorageType.AzureCosmosDb)
                {
                    AzureBlobContainerPath = "Test Please Ignore",
                    AzureBlobContainerTargetUri = new Uri("https://tempuri.org/exportme")
                };
            }
            else if (capability == PcfTestCapability.AccountClose)
            {
                privacyCommand = new AccountCloseCommand(
                    agentId: agentId,
                    assetGroupId: assetGroupInfo.AssetGroupId,
                    assetGroupQualifier: assetGroupInfo.AssetGroupQualifier,
                    verifier: string.Empty,
                    verifierV3: string.Empty,
                    batchId: new RequestBatchId(Guid.NewGuid()),
                    clientCommandState: "Test Please Ignore",
                    commandId: new CommandId(Guid.NewGuid().ToString()),
                    nextVisibleTime: DateTimeOffset.FromUnixTimeSeconds(100).ToNearestMsUtc(),
                    subject: pcfTestSubjectType.GetPrivacySubject(),
                    correlationVector: "Test Please Ignore",
                    timestamp: DateTimeOffset.FromUnixTimeSeconds(100).ToNearestMsUtc(),
                    cloudInstance: Policies.Current.CloudInstances.Ids.Public.Value,
                    commandSource: string.Empty,
                    processorApplicable: true,
                    controllerApplicable: true,
                    absoluteExpirationTime: DateTimeOffset.UtcNow.AddDays(1),
                    queueStorageType: QueueStorageType.AzureCosmosDb);
            }
            else
            {
                throw new ArgumentException($"Unknown command capability: {capability}.");
            }

            if (privacyCommand.Subject is AadSubject && !string.IsNullOrEmpty(tenantid))
            {
                (privacyCommand.Subject as AadSubject).TenantId = Guid.Parse(tenantid);
            }

            return privacyCommand;
        }
    }
}
