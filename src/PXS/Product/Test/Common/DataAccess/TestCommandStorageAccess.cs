// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Test.Common.DataAccess
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///     Test-CommandStorageAccess is only used for storing test commands in test environments
    /// </summary>
    public class TestCommandStorageAccess : ICommandStorage
    {
        private readonly ITable<PrivacyCommandEntity> commandEntityTable;

        private readonly ITable<DeadLetterStorage<AccountCloseRequest>> deadLetterTable;

        public TestCommandStorageAccess(ITable<PrivacyCommandEntity> commandEntityTable, ITable<DeadLetterStorage<AccountCloseRequest>> deadLetterTable)
        {
            this.commandEntityTable = commandEntityTable;
            this.deadLetterTable = deadLetterTable;
        }

        /// <summary>
        ///     Returns the first command matching the object id and tenant id
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="tenantId"></param>
        /// <remarks>
        ///     Depending on the scenario, this may not be the API you want to use. For account close,
        ///     there should be only one command per object id + tenant id, so it works fine in that case.
        /// </remarks>
        /// <returns>PrivacyRequest</returns>
        public async Task<PrivacyRequest> ReadPrivacyRequestFirstOrDefaultAsync(Guid objectId, Guid tenantId)
        {
            string query = $"PartitionKey eq '{objectId.ToString()}' and AadTenantId eq guid'{tenantId.ToString()}'";
            PrivacyCommandEntity result = (await this.commandEntityTable.QueryAsync(query)
                .ConfigureAwait(false)).FirstOrDefault();
            return result?.DataActual;
        }

        public async Task<PrivacyRequest> ReadPrivacyRequestFirstOrDefaultAsync(long msaPuid)
        {
            var query = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, msaPuid.ToString());
            PrivacyCommandEntity result = (await this.commandEntityTable.QueryAsync(query)).FirstOrDefault();

            return result?.DataActual;
        }

        public Task<bool> WritePrivacyRequestAsync(PrivacyRequest request)
        {
            return this.commandEntityTable.InsertAsync(new PrivacyCommandEntity(request));
        }

        public async Task<DeadLetterStorage<AccountCloseRequest>> ReadDeadLetterItemAsync(string tenantId, string objectId)
        {
            return await deadLetterTable.GetItemAsync(partitionId: tenantId.ToString(), rowId: objectId.ToString()).ConfigureAwait(false);
        }
    }
}
