// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.SecretStore
{
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     ISecretStoreReader
    /// </summary>
    public interface ISecretStoreReader
    {
        /// <summary>
        ///     Given a secret name, reads the secret data.
        /// </summary>
        /// <param name="secretName">The secret name to read.</param>
        /// <returns>The encrypted data.</returns>
        Task<string> ReadSecretByNameAsync(string secretName);

        /// <summary>
        ///     Given a name, read the current version.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="includePrivateKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<X509Certificate2> GetCertificateCurrentVersionAsync(
            string name,
            bool includePrivateKey = false,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
