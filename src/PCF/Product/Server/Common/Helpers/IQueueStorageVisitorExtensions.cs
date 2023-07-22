// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    public static class IQueueStorageVisitorExtensions
    {
        /// <summary>
        /// A unified classify-then-visit implementation.
        /// </summary>
        public static TReturn Process<TReturn>(this IQueueStorageVisitor<TReturn> visitor, QueueStorageType queueStorageType)
        {
            switch (queueStorageType)
            {
                case QueueStorageType.AzureCosmosDb:
                    return visitor.VisitAzureCosmosDb();

                case QueueStorageType.AzureQueueStorage:
                    return visitor.VisitAzureQueueStorage();

                default:
                    return visitor.VisitDefault();
            }
        }
    }
}
