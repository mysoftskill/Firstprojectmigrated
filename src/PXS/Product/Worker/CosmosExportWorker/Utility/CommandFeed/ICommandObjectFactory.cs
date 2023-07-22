// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using Microsoft.PrivacyServices.CommandFeed.Client;

    /// <summary>
    ///     interface for creating PCF objects for the Cosmos Export Task
    /// </summary>
    public interface ICommandObjectFactory
    {
        /// <summary>
        ///     Creates the PCF data agent for processing Cosmos requests
        /// </summary>
        /// <param name="taskId">task id</param>
        /// <returns>resulting value</returns>
        IPrivacyDataAgent CreateDataAgent(string taskId);

        /// <summary>
        ///     Creates a logger that the PCF client will invoke when certain events occur
        /// </summary>
        /// <returns>resulting value</returns>
        CommandFeedLogger CreateLogger();

        /// <summary>
        ///     Creates a command feed client
        /// </summary>
        /// <returns>resulting value</returns>
        ICommandClient CreateCommandFeedClient();

        /// <summary>
        ///     Creates a command receiver
        /// </summary>
        /// <param name="taskId">task id</param>
        /// <returns>resulting value</returns>
        ICommandReceiver CreateCommandReceiver(string taskId);
    }
}
