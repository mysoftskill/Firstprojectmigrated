// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Host
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    ///     HostApplication
    /// </summary>
    public abstract class HostApplication : IDisposable
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        ///     The currently running application. This is a singleton that is representative of the current process.
        /// </summary>
        public static HostApplication Instance { get; private set; }

        /// <summary>
        ///     Cancellation Token
        /// </summary>
        public CancellationToken CancellationToken => this.cancellationTokenSource.Token;

        /// <inheritdoc />
        public void Dispose()
        {
            this.cancellationTokenSource?.Dispose();
        }

        /// <summary>
        ///     Initializes a new instance of the HostApplication class
        /// </summary>
        protected HostApplication()
        {
            Instance = this;
        }

        /// <summary>
        ///     Stops the app without a control+c event.
        /// </summary>
        protected void Shutdown()
        {
            this.cancellationTokenSource.Cancel();

            Trace.Flush();
        }
    }
}
