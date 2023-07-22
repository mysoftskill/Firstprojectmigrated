// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;

    /// <summary>
    ///     Request Context
    /// </summary>
    public class RequestContext : IRequestContext
    {
        /// <summary>
        ///     This exists as a refactoring used by old unit tests, which were not written to construct an
        ///     identity to pass to a RequestContext and assume the RequestContext is the owner of identity.
        ///     DO NOT USE THIS FOR NEW CODE, EVEN TESTS.
        /// </summary>
        /// <remarks>
        ///     I would like to inline this at all the call sites, but there are quite a few. For now will
        ///     leave this as a bit of a factory method, but please do not use it.
        /// </remarks>
        public static RequestContext CreateOldStyle(
            Uri currentUri,
            string userProxyTicket,
            string familyJsonWebToken,
            long authorizingPuid,
            long targetPuid,
            long? authorizingCid,
            long? targetCid,
            string countryRegion,
            string callerName,
            long callerSiteId,
            string[] flights,
            bool isWatchdogRequest = false,
            LegalAgeGroup legalAgeGroup = LegalAgeGroup.Undefined)
        {
            var identity = new MsaSelfIdentity(
                userProxyTicket,
                familyJsonWebToken,
                authorizingPuid,
                targetPuid,
                authorizingCid,
                callerName,
                callerSiteId,
                targetCid,
                countryRegion,
                null,
                false,
                AuthType.MsaSelf,
                legalAgeGroup);

            var headers = new Dictionary<string, string[]>();
            if (flights != null && flights.Length > 0)
                headers.Add(HeaderNames.Flights, flights);
            if (isWatchdogRequest)
                headers.Add(HeaderNames.WatchdogRequest, new[] { true.ToString() });

            return new RequestContext(
                identity,
                currentUri,
                headers);
        }

        /// <inheritdoc />
        public string ClientRequestId
        {
            get
            {
                this.Headers.TryGetValue(HeaderNames.ClientRequestId, out string[] clientRequestIds);
                return clientRequestIds?.FirstOrDefault();
            }
        }

        /// <inheritdoc />
        public Uri CurrentUri { get; }

        /// <inheritdoc />
        public string[] Flights
        {
            get
            {
                if (!this.Headers.TryGetValue(HeaderNames.Flights, out string[] headerValues))
                    return new string[0];

                return headerValues
                    .SelectMany(f => f.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    .ToArray();
            }
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string[]> Headers { get; }

        /// <inheritdoc />
        public IIdentity Identity { get; }

        /// <inheritdoc />
        public bool IsWatchdogRequest
        {
            get
            {
                if (!this.Headers.TryGetValue(HeaderNames.WatchdogRequest, out var headerValues))
                    return false;

                string value = headerValues.SingleOrDefault();
                return string.Equals(true.ToString(), value, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <inheritdoc />
        public long? TargetCid => this.GetIdentityValueOrDefault<MsaSelfIdentity, AadIdentityWithMsaUserProxyTicket, long?>(i => i.TargetCid, i => i.TargetCid);

        /// <inheritdoc />
        public long TargetPuid => this.GetIdentityValueOrDefault<MsaSelfIdentity, AadIdentityWithMsaUserProxyTicket, long>(i => i.TargetPuid, i => i.TargetPuid);

        /// <summary>
        ///     Creates a new <see cref="RequestContext" />
        /// </summary>
        /// <param name="identity">identity</param>
        /// <param name="requestUri">current request uri</param>
        /// <param name="headers">headers</param>
        public RequestContext(IIdentity identity, Uri requestUri = null, IReadOnlyDictionary<string, string[]> headers = null)
        {
            // These lines should be all we need. For now we are copying the data out of the identity into first class properties,
            // but the next step is to refactor all the properties on this class to simply poke into the identity and/or headers, and
            // then to inline at call sites to just look at RequestContext.Identity directly.
            this.CurrentUri = requestUri;
            this.Headers = headers ?? new Dictionary<string, string[]>();

            this.Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        /// <inheritdoc />
        public TV GetIdentityValueOrDefault<TI, TV>(Func<TI, TV> getFunc)
            where TI : IIdentity
        {
            if (!(this.Identity is TI identity))
                return default(TV);

            return getFunc(identity);
        }

        /// <inheritdoc />
        public TV GetIdentityValueOrDefault<TI1, TI2, TV>(Func<TI1, TV> getFunc1, Func<TI2, TV> getFunc2)
            where TI1 : IIdentity
            where TI2 : IIdentity
        {
            if (this.Identity is TI1 identity1)
                return getFunc1(identity1);

            if (this.Identity is TI2 identity2)
                return getFunc2(identity2);

            return default(TV);
        }

        public Portal GetPortal(IDictionary<string, string> siteIdToCallerName)
        {
            if (this.Identity is AadIdentity aadIdentity)
            {
                if (siteIdToCallerName.TryGetValue(aadIdentity.ApplicationId, out string siteName))
                {
                    if (siteName.ToUpperInvariant().StartsWith("PCD"))
                        return Portal.Pcd;
                    if (siteName.ToUpperInvariant().StartsWith("MSGRAPH"))
                        return Portal.MsGraph;
                    if (siteName.ToUpperInvariant().StartsWith("AADPXSTEST"))
                        return Portal.PxsAadTest;
                    if (siteName.ToUpperInvariant().StartsWith("BING"))
                        return Portal.Bing;
                }
            }

            if (this.Identity is MsaSiteIdentity msaIdentity)
            {
                if (siteIdToCallerName.TryGetValue(msaIdentity.CallerMsaSiteId.ToString(), out string siteName))
                {
                    if (siteName.ToUpperInvariant().StartsWith("MEEPORTAL"))
                        return Portal.Amc;
                    if (siteName.ToUpperInvariant().StartsWith("PXSTEST"))
                        return Portal.PxsTest;
                }
            }

            return Portal.Unknown;
        }

        /// <inheritdoc />
        public T RequireExactIdentity<T>()
            where T : IIdentity
        {
            if (this.Identity.GetType() != typeof(T))
                throw new ArgumentOutOfRangeException(nameof(T), $"Expected identity of type {typeof(T).FullName} but was of type {this.Identity?.GetType().FullName}");

            return (T)this.Identity;
        }

        /// <inheritdoc />
        public T RequireIdentity<T>()
            where T : IIdentity
        {
            if (this.Identity is T identity)
                return identity;

            throw new ArgumentOutOfRangeException(nameof(T), $"Expected identity derived from type {typeof(T).FullName} but was of type {this.Identity?.GetType().FullName}");
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{RequestContext: {this.CurrentUri} {this.Identity}}}";
        }
    }
}
