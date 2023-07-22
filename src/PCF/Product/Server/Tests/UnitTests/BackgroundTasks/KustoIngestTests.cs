namespace PCF.UnitTests
{
    using System;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Moq;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class KustoIngestTests : INeedDataBuilders
    {
        [Fact]
        public async Task EnsureKustoIngestedToCorrectTable()
        {
            AgentId AgentId = this.AnAgentId();
            CommandId CommandId = this.ACommandId();
            string FilePath = "file001/testfile.json";

            Mock<ICommandHistoryRepository> mockCommandHistoryRepository = new Mock<ICommandHistoryRepository>();
            Mock<IKustoClient> mockKustoClient = new Mock<IKustoClient>();

            var IngestionTime = DateTime.UtcNow;
            var items = new []
            {
                new
                {
                    AgentId,
                    CommandId,
                    FilePath,
                    IngestionTime
                }
            };

            var dataReader = mockKustoClient.Object.CreateDataReader(
            items.Select(x => new
            {
                AgentId,
                CommandId,
                FilePath,
                IngestionTime
            }),
            nameof(AgentId),
            nameof(CommandId),
            nameof(FilePath),
            nameof(IngestionTime));

            mockKustoClient.Setup(a => a.CreateDataReader(items.Select(x => new
            {
                AgentId,
                CommandId,
                FilePath,
                IngestionTime
            }))).Returns(dataReader);

            var archive = new ExportArchive(null, null, mockKustoClient.Object);
            await archive.IngestToKustoTelemetry(AgentId, CommandId, FilePath, IngestionTime);

            mockKustoClient.Verify(a => a.IngestAsync("PCFAgentsWithMalwareInMSAExports", dataReader, true), Times.Once);
        }
    }
}