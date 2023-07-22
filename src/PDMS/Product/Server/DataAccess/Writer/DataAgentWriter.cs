namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Provides methods for writing data agents.
    /// </summary>
    public class DataAgentWriter : IDataAgentWriter
    {
        private readonly IDeleteAgentWriter deleteAgentWriter;
        private readonly IDataAgentReader dataAgentReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAgentWriter"/> class.
        /// </summary>
        /// <param name="deleteAgentWriter">The delete agent writer.</param>
        /// <param name="dataAgentReader">The data agent reader.</param>
        public DataAgentWriter(
            IDeleteAgentWriter deleteAgentWriter,
            IDataAgentReader dataAgentReader)
        {
            this.deleteAgentWriter = deleteAgentWriter;
            this.dataAgentReader = dataAgentReader;
        }

        /// <summary>
        /// Creates an entity.
        /// </summary>
        /// <param name="entity">Entity to be persisted.</param>
        /// <returns>The Task object for asynchronous execution.</returns>
        public async Task<DataAgent> CreateAsync(DataAgent entity)
        {
            var deleteAgent = entity as DeleteAgent;

            if (deleteAgent != null)
            {
                return await this.deleteAgentWriter.CreateAsync(deleteAgent).ConfigureAwait(false);
            }

            throw new NotImplementedException($"Uknown AgentType for CreateAsync: {entity.GetType()}");
        }

        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">Entity with updated properties.</param>
        /// <returns>The Task object for asynchronous execution.</returns>
        public async Task<DataAgent> UpdateAsync(DataAgent entity)
        {
            var deleteAgent = entity as DeleteAgent;

            if (deleteAgent != null)
            {
                return await this.deleteAgentWriter.UpdateAsync(deleteAgent).ConfigureAwait(false);
            }

            throw new NotImplementedException($"Uknown AgentType for CreateAsync: {entity.GetType()}");
        }

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="id">The id of the entity.</param>
        /// <param name="etag">The ETag of the resource.</param>
        /// <param name="overridePendingCommandsCheck">The override flag for pending commands check.</param>
        /// <param name="force">The flag to force delete.</param>
        /// <returns>The Task object for asynchronous execution.</returns>
        public async Task DeleteAsync(Guid id, string etag, bool overridePendingCommandsCheck, bool force)
        {
            var agent = await this.dataAgentReader.ReadByIdAsync(id, ExpandOptions.None).ConfigureAwait(false);

            var deleteAgent = agent as DeleteAgent;

            if (deleteAgent != null)
            {
                await this.deleteAgentWriter.DeleteAsync(id, etag, overridePendingCommandsCheck, force).ConfigureAwait(false);
                return;
            }

            throw new EntityNotFoundException(id, typeof(DataAgent).Name);
        }

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="id">The id of the entity.</param>
        /// <param name="etag">The ETag of the resource.</param>
        /// <returns>The Task object for asynchronous execution.</returns>
        public Task DeleteAsync(Guid id, string etag) => this.DeleteAsync(id, etag, false, false);
    }
}