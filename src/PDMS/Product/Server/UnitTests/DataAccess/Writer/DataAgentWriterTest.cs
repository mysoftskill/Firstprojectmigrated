namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class DataAgentWriterTest
    {
        public static IDictionary<ReleaseState, ConnectionDetail> CreateConnectionDetails(ConnectionDetail connectionDetails)
        {
            return new Dictionary<ReleaseState, ConnectionDetail> { { connectionDetails.ReleaseState, connectionDetails } };
        }

        [Theory(DisplayName = "When CreateAsync is called for DeleteAgent, then the correct interface is called."), AutoMoqData(true)]
        public async Task When_CreateAsyncDeleteAgent_Then_CallIDeleteAgentWriter(
            [Frozen] Mock<IDeleteAgentWriter> typedWriter,
            DeleteAgent dataAgent,
            DataAgentWriter writer)
        {
            var result = await writer.CreateAsync(dataAgent).ConfigureAwait(false);

            Assert.IsType<DeleteAgent>(result);

            typedWriter.Verify(m => m.CreateAsync(dataAgent), Times.Once);
        }

        [Theory(DisplayName = "When UpdateAsync is called for DeleteAgent, then the correct interface is called."), AutoMoqData(true)]
        public async Task When_UpdateAsyncDeleteAgent_Then_CallIDeleteAgentWriter(
            [Frozen] Mock<IDeleteAgentWriter> typedWriter,
            DeleteAgent dataAgent,
            DataAgentWriter writer)
        {
            var result = await writer.UpdateAsync(dataAgent).ConfigureAwait(false);

            Assert.IsType<DeleteAgent>(result);

            typedWriter.Verify(m => m.UpdateAsync(dataAgent), Times.Once);
        }

        [Theory(DisplayName = "When DeleteAsync is called for DeleteAgent, then the DeleteAgentWriter is called."), AutoMoqData(true)]
        public async Task When_DeleteAsyncForDeleteAgent_Then_CallIDeleteAgentWriter(
            Guid id,
            string etag,
            [Frozen] Mock<IDeleteAgentWriter> typedWriter,
            [Frozen] Mock<IDataAgentReader> dataAgentReader,
            DataAgentWriter writer)
        {
            dataAgentReader.Setup(m => m.ReadByIdAsync(id, ExpandOptions.None)).ReturnsAsync(new DeleteAgent());

            await writer.DeleteAsync(id, etag).ConfigureAwait(false);

            typedWriter.Verify(m => m.DeleteAsync(id, etag, false, false), Times.Once);
        }

        [Theory(DisplayName = "When DeleteAsync is called for non existing entity, then fail."), AutoMoqData(true)]
        public async Task When_DeleteAsyncForNonExisting_Then_Fail(
            Guid id,
            string etag,
            [Frozen] Mock<IDataAgentReader> dataAgentReader,
            DataAgentWriter writer)
        {
            dataAgentReader.Setup(m => m.ReadByIdAsync(id, ExpandOptions.None)).ReturnsAsync(null as DataAgent);
            
            await Assert.ThrowsAsync<EntityNotFoundException>(() => writer.DeleteAsync(id, etag)).ConfigureAwait(false);
        }
    }
}