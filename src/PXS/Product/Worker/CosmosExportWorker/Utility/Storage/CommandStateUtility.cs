// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.PrivacyServices.CommandFeed.Client;

    /// <summary>
    ///     enum representing what happened after attempting to insert or fetch a command state row
    /// </summary>
    public enum InsertOrFetch
    {
        /// <summary>
        ///     an error occurred
        /// </summary>
        Error = 0,

        /// <summary>
        ///     the row was inserted 
        /// </summary>
        Inserted,

        /// <summary>
        ///     the row was fetched
        /// </summary>
        Fetched,
    }

    /// <summary>
    ///     utiltiy functions for command state 
    /// </summary>
    public static class CommandStateUtility
    {
        /// <summary>
        ///     Adds or fetches state for the command from the table store
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <param name="commandState">command state table accessor</param>
        /// <param name="commandId">command id</param>
        /// <param name="agentId">agent id command is for</param>
        /// <param name="leaseReceipt">lease receipt</param>
        /// <param name="isTestCommand">true if this is a test command; false otherwise</param>
        /// <param name="isComplete">true if the command is complete; false otherwise</param>
        /// <param name="isNotApplicable">true if the command is not applicable to the agent; false otherwise</param>
        /// <param name="tryInsertFirst">true to try inserting first; false to try fetching first</param>
        /// <returns>value indicating whether the state was inserted or fetched; the inserted or fetched command state</returns>
        public static async Task<(InsertOrFetch Status, CommandState State)> InsertOrFetchCommandStateAsync(
            OperationContext ctx,
            ITable<CommandState> commandState,
            string commandId,
            string agentId,
            string leaseReceipt,
            bool isTestCommand,
            bool isComplete,
            bool isNotApplicable,
            bool tryInsertFirst)
        {
            bool attemptedInsert = false;
            bool tryInsert = tryInsertFirst;

            // this is messy, but the goal is to avoid unnecessary network calls to storage.
            //  a row is expected to exist if we have agent state, so in that case, we want to try a get first, but we do want to
            //   allow for an error where we have agent state but failed to write to storage, so we allow ourselves to try an insert 
            //   as a backup.  If two threads are attempting to insert at the same time, we could get a conflict, so allow a final 
            //   get as a backup to the insert.
            //  a row is not expected to exist if we have no agent state, so in that case, we want to try to insert first.  However,
            //   we do want to allow for an error where we wrote to storage but failed to update agent state, so we allow for a
            //   get following the insert.  But if we attempted an insert and failed once, we don't want to bother attempting the 
            //   insert again
            //  It is expected that if insert fails, the next get will succeed because the only non-fatal insert error is 'already
            //   exists', so the expected patterns of get & insert are:
            //   Insert -> Get
            //   Get -> Insert
            //   Get -> Insert -> Get

            for (int i = 0; i < 2 && attemptedInsert == false; ++i)
            {
                CommandState state;
                CommandState query;

                if (tryInsert)
                {
                    state = new CommandState
                    {
                        AgentId = agentId,
                        CommandId = commandId,

                        LeaseReceipt = leaseReceipt,

                        NonTransientError = false,
                        
                        IgnoreCommand = isTestCommand,
                        NotApplicable = isNotApplicable,
                        IsComplete = isComplete,
                    };

                    ctx.Op = "inserting new command into table";

                    if (await commandState.InsertAsync(state).ConfigureAwait(false))
                    {
                        return (InsertOrFetch.Inserted, state);
                    }

                    attemptedInsert = true;
                }

                query = new CommandState { AgentId = agentId, CommandId = commandId };

                ctx.Op = "fetching existing item from table to check for complete state";

                state = await commandState.GetItemAsync(query.PartitionKey, query.RowKey).ConfigureAwait(false);
                if (state != null)
                {
                    return (InsertOrFetch.Fetched, state);
                }

                // if we tried fetching first, allow us to try an insert 
                tryInsert = true;
            }

            return (InsertOrFetch.Error, null);
        }
    }
}
