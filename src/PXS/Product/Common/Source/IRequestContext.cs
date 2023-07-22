// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Collections.Generic;
    using System.Security.Principal;

    /// <summary>
    ///     Context of the user request
    /// </summary>
    public interface IRequestContext
    {
        /// <summary>
        ///     Gets the client request id.
        /// </summary>
        string ClientRequestId { get; }

        /// <summary>
        ///     Gets the current request Uri. Used for construction of nextlinks.
        /// </summary>
        Uri CurrentUri { get; }

        /// <summary>
        ///     The list of flights the user is on
        /// </summary>
        string[] Flights { get; }

        /// <summary>
        ///     The headers on the request.
        /// </summary>
        IReadOnlyDictionary<string, string[]> Headers { get; }

        /// <summary>
        ///     The identity in the request.
        /// </summary>
        IIdentity Identity { get; }

        /// <summary>
        ///     Gets a value indicating whether this request originated from a watchdog test.
        /// </summary>
        bool IsWatchdogRequest { get; }

        /// <summary>
        ///     Gets the cid of the target of the request
        /// </summary>
        long? TargetCid { get; }

        /// <summary>
        ///     Get the puid of the target of the request
        /// </summary>
        long TargetPuid { get; }

        /// <summary>
        ///     Gets the value on an identity of a given type, or if the identity is not that type, a default value.
        /// </summary>
        /// <typeparam name="TI">The <see cref="IIdentity" /> type.</typeparam>
        /// <typeparam name="TV">The type of value returned.</typeparam>
        /// <param name="getFunc">The method for fetching the value off the identity.</param>
        TV GetIdentityValueOrDefault<TI, TV>(Func<TI, TV> getFunc)
            where TI : IIdentity;

        /// <summary>
        ///     Gets the value on an identity of a given type or backup type, or if the identity is not those types, a default value.
        /// </summary>
        /// <typeparam name="TI1">The primary <see cref="IIdentity" /> type.</typeparam>
        /// <typeparam name="TI2">The backup <see cref="IIdentity" /> type.</typeparam>
        /// <typeparam name="TV">The type of value returned.</typeparam>
        /// <param name="getFunc1">The method for fetching the value off the identity.</param>
        /// <param name="getFunc2">The method for fetching the value off the identity.</param>
        TV GetIdentityValueOrDefault<TI1, TI2, TV>(Func<TI1, TV> getFunc1, Func<TI2, TV> getFunc2)
            where TI1 : IIdentity
            where TI2 : IIdentity;

        /// <summary>
        ///     Get the portal for this request context.
        /// </summary>
        /// <param name="siteIdToCallerName">Site Id to Caller Name Map</param>
        Portal GetPortal(IDictionary<string, string> siteIdToCallerName);

        /// <summary>
        ///     Gets the identity on the request context, as a particular <see cref="IIdentity" /> type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IIdentity" /> to retrieve.</typeparam>
        T RequireExactIdentity<T>()
            where T : IIdentity;

        /// <summary>
        ///     Gets the identity on the request context, as a particular <see cref="IIdentity" /> type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IIdentity" /> to retrieve.</typeparam>
        T RequireIdentity<T>()
            where T : IIdentity;
    }
}
