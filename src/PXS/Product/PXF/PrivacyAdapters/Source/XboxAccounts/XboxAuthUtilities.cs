// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts
{
    using System;
    using System.Collections.Specialized;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    using Microsoft.XboxLive.Auth.Json;
    using Microsoft.XboxLive.Auth.Keys;
    using Microsoft.XboxLive.Auth.Keys.Jwk;
    using Microsoft.XboxLive.Auth.ProofKeys;

    internal static class XboxAuthUtilities
    {
        internal static async Task<string> CreateSignature(HttpRequestMessage request, SignaturePolicy signaturePolicy, CngKey privateSigningKey)
        {
            var signingContext = new SigningContext(CngAlgorithm.Sha256, new ECDsaCng(privateSigningKey));
            long timestamp = DateTime.UtcNow.ToFileTimeUtc();
            byte[] requestContent = await request.Content.ReadAsByteArrayAsync();
            ProofKeyUtil.SignRequest(
                context: signingContext,
                policy: signaturePolicy,
                timestamp: timestamp,
                method: request.Method.ToString().ToUpperInvariant(),
                pathAndQuery: request.RequestUri.PathAndQuery,
                headers: new NameValueCollection(),
                body: requestContent,
                index: 0,
                count: requestContent.Length);
            return ProofKeyUtil.CreateSignatureHeader(signature: signingContext.GetSignature(), version: signaturePolicy.Version, timestamp: timestamp);
        }

        internal static EccJsonWebKey ToProofKey(this CngKey privateSigningKey)
        {
            using (var signingAlgorithm = new ECDsaCng(privateSigningKey))
            {
                return new EccJsonWebKey(signingAlgorithm)
                {
                    Use = JsonWebKeyUse.Signing,
                    Algorithm = JsonWebSigningAlgorithms.ECDSASHA256
                };
            }
        }
    }
}
