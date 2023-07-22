// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    /// <summary>
    /// Selects <see cref="QueueStorageType"/> based on information within the <see cref="PrivacyCommand"/> and against flights and/or configuration
    /// </summary>
    public class QueueStorageTypeSelector : ICommandVisitor<PrivacyCommand, QueueStorageType>
    {
        /// <inheritdoc />
        public PrivacyCommandType Classify(PrivacyCommand command)
        {
            return command.CommandType;
        }

        /// <inheritdoc />
        public QueueStorageType VisitAccountClose(PrivacyCommand accountCloseCommand)
        {
            return QueueStorageType.AzureCosmosDb;
        }

        /// <inheritdoc />
        public QueueStorageType VisitAgeOut(PrivacyCommand ageOutCommand)
        {
            if (ageOutCommand.Subject is MsaSubject)
            {
                return QueueStorageType.AzureQueueStorage;
            }

            return QueueStorageType.AzureCosmosDb;
        }

        /// <inheritdoc />
        public QueueStorageType VisitDelete(PrivacyCommand deleteCommand)
        {
            return QueueStorageType.AzureCosmosDb;
        }

        /// <inheritdoc />
        public QueueStorageType VisitExport(PrivacyCommand exportCommand)
        {
            return QueueStorageType.AzureCosmosDb;
        }
    }
}
