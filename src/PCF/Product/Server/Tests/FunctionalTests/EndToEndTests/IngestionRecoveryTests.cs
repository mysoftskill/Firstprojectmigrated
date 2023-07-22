namespace PCF.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    using Newtonsoft.Json;

    using Xunit;
    using Xunit.Abstractions;

    using DeleteCommand = Microsoft.PrivacyServices.CommandFeed.Client.DeleteCommand;
    using PXSV1 = Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

#if INCLUDE_TEST_HOOKS

    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    [Trait("Category", "FCT")]
    public class IngestionRecoveryTests : EndToEndTestBase
    {
        // These values correspond to values in the INT configuration that are hardcoded. Test AgentId 8
        private static readonly Guid AgentId = Guid.Parse("30d4f43210a54284a8de7dec2435ae46");

        // Supports:
        // MSA browsing history deletes
        private static readonly Guid AssetGroupId = Guid.Parse("c992ee4799854378940b859ecc6e6ce1");

        // Supports all 5 data types with custom predicates, all subject types, and all command types.
        private const string AssetGroupQualifier = "AssetType=AzureTable;AccountName=cf3f402d-aa79-48d8-8fcd-8cb8bef4b4f1;TableName=dc5a1a10-b4f2-49a2-b7c5-0f429e5ef80e";

        /// <summary>
        /// Ingestion recovery tests
        /// </summary>
        public IngestionRecoveryTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Theory]
        [AutoMoqDeleteCommand(DataTypeId = "BrowsingHistory", SubjectType = typeof(MsaSubject))]
        public async Task CommandIngestionRecovery(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupId = AssetGroupId.ToString("n");

            var pxsDeleteCommand = PopulateRequest<PXSV1.DeleteRequest>(deleteCommand, PXSV1.RequestType.Delete);

            pxsDeleteCommand.TimeRangePredicate = deleteCommand.TimeRangePredicate;
            pxsDeleteCommand.PrivacyDataType = deleteCommand.PrivacyDataType.Value;
            pxsDeleteCommand.Predicate = deleteCommand.DataTypePredicate;
            pxsDeleteCommand.Requester = TestCommandRequester;
            pxsDeleteCommand.Context = TestCommandContext;

            var requestContent = new Dictionary<string, string>
            {
                ["agentId"] = AgentId.ToString("n"),
                ["assetGroupId"] = AssetGroupId.ToString("n"),
                ["assetGroupQualifier"] = AssetGroupQualifier,
                ["pxsCommand"] = JsonConvert.SerializeObject(pxsDeleteCommand),
                ["queueStorageType"] = QueueStorageType.AzureCosmosDb.ToString(),
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{TestSettings.ApiHostName}/testhooks/backgroundtasks/ingestionRecovery/")
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json")
            };

            var response = await TestSettings.SendWithS2SAync(request, this.outputHelper);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            this.OutputHelper.WriteLine("Receive command...");
            await this.ReceiveAsync<DeleteCommand>(AgentId, AssetGroupId, pxsDeleteCommand, null);
        }
    }

#endif

}
