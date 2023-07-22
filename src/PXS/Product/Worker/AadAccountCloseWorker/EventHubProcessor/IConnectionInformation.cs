// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor
{
    /// <summary>
    ///     Contains information required for connecting to an Azure Event Hub
    /// </summary>
    public interface IConnectionInformation
    {
        /// <summary>
        ///     Gets a specific value from the <see cref="IConnectionInformation"/>
        /// </summary>
        /// <param name="key">The key name of the value desired</param>
        /// <returns>The value as a string</returns>
        string this[string key] { get; }

        /// <summary>
        ///     Gets the connection string for the Azure Event Hub
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        ///     Gets the Azure Event Hub Endpoint
        /// </summary>
        string Endpoint { get; }

        /// <summary>
        ///     Gets the Azure Event Hub Entity Path
        /// </summary>
        string EntityPath { get; }

        /// <summary>
        ///     Gets the name identifier for the queue
        /// </summary>
        string Name { get; }
    }
}