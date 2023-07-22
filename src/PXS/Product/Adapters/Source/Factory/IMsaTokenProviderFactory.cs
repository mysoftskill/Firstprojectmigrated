// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Factory
{
    using Microsoft.Membership.MemberServices.Contracts.Adapter.MsaTokenProvider;

    public interface IMsaTokenProviderFactory
    {
        /// <summary>
        /// Returns the MSA token provider for a given MSA target scope.
        /// </summary>
        /// <param name="targetScope">The MSA target scope identifying the partner to which tokens will be sent.</param>
        /// <returns>The MSA token provider. Null if no MsaTokenProvider exists for the target scope.</returns>
        IMsaTokenProvider GetTokenProvider(string targetScope);
    }
}
