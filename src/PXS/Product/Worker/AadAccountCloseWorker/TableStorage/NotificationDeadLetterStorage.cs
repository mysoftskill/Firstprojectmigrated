// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage
{
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;

    /// <summary>
    /// </summary>
    public class NotificationDeadLetterStorage : DeadLetterStorage<Notification>
    {
    }
}
