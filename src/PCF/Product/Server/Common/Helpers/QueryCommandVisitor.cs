// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// Visitor pattern to define behavior for QueryCommand support based on the <see cref="QueueStorageType"/>
    /// </summary>
    public class QueryCommandVisitor : IQueueStorageVisitor<bool>
    {
        public bool VisitAzureCosmosDb()
        {
            return true;
        }

        public bool VisitAzureQueueStorage()
        {
            return false;
        }

        public bool VisitDefault()
        {
            return true;
        }
    }
}
