// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors
{
    using Microsoft.Membership.MemberServices.Configuration;

    internal interface IUserCreateEventProcessorFactory
    {
        IUserCreateEventProcessor Create(IAqsQueueProcessorConfiguration config);
    }
}
