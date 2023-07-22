// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    ///     Processor for <see cref="CDPEvent2"/> containing <see cref="UserCreate"/> <see cref="EventData"/>.
    /// </summary>
    public interface IUserCreateEventProcessor
    {
        /// <summary>
        ///     Processes <see cref="CDPEvent2"/>s into <see cref="AccountCreateInformation"/>s.
        /// </summary>
        /// <param name="createItems">The events containing <see cref="EventData"/> of the type <see cref="UserCreate"/></param>
        /// <returns>A collection of <see cref="AccountCreateInformation"/> for the <see cref="CDPEvent2"/> that could be successfully processed</returns>
        Task<AdapterResponse<IList<AccountCreateInformation>>> ProcessCreateItemsAsync(IEnumerable<CDPEvent2> createItems);
    }
}
