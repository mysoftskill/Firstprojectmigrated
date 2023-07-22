// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    ///     Processor for <see cref="CDPEvent2"/> containing <see cref="UserDelete"/> <see cref="EventData"/>.
    /// </summary>
    public interface IUserDeleteEventProcessor
    {
        /// <summary>
        ///     Gets the <see cref="CdpEvent2Helper"/> used for parsing <see cref="CDPEvent2"/>s.
        /// </summary>
        CdpEvent2Helper EventHelper { get; }

        /// <summary>
        ///     Processes <see cref="CDPEvent2"/>s into <see cref="AccountDeleteInformation"/>s.
        /// </summary>
        /// <param name="evt">The <see cref="CDPEvent2"/> that contains <see cref="EventData"/> of the type <see cref="UserDelete"/></param>
        /// <param name="token">Cancellation token</param>
        /// <returns><see cref="AccountDeleteInformation"/> gathered from and for the <see cref="CDPEvent2"/></returns>
        Task<AdapterResponse<IList<AdapterResponse<AccountDeleteInformation>>>> ProcessDeleteItemsAsync(IEnumerable<CDPEvent2> evt, CancellationToken token = default(CancellationToken));
    }
}
