//--------------------------------------------------------------------------------
// <copyright file="WebTransientErrorDetectionStrategy.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Net;
    using Microsoft.Practices.TransientFaultHandling;

    /// <summary>
    /// Web transient-error detection-strategy.
    /// </summary>
    public class WebTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        /// <summary>
        /// The (singleton) instance of <see cref="WebTransientErrorDetectionStrategy"/>.
        /// </summary>
        private static readonly Lazy<WebTransientErrorDetectionStrategy> instance = new Lazy<WebTransientErrorDetectionStrategy>(() => new WebTransientErrorDetectionStrategy());

        /// <summary>
        /// Gets the singleton instance of <see cref="WebTransientErrorDetectionStrategy"/>.
        /// </summary>
        public static WebTransientErrorDetectionStrategy Instance
        {
            get { return WebTransientErrorDetectionStrategy.instance.Value; }
        }

        /// <summary>
        /// Determines whether an exception is due to a transient error.
        /// </summary>
        /// <param name="ex">The exception to review.</param>
        /// <returns>true if exception is caused by transient issue; otherwise false.</returns>
        public bool IsTransient(Exception ex)
        {
            WebException webException;
            if (TryGetWebException(ex, out webException))
            {
                return IsRetryableWebException(webException);
            }

            return false;
        }

        /// <summary>
        /// Recursively access the inner-exceptions of <paramref name="ex"/>, until a <seealso cref="WebException"/> is found.
        /// </summary>
        /// <param name="ex">The exception to parse.</param>
        /// <param name="webException">The output web-exception.</param>
        /// <returns>true on success; otherwise false.</returns>
        private static bool TryGetWebException(Exception ex, out WebException webException)
        {
            webException = ex as WebException;

            if (webException == null && ex.InnerException != null)
            {
                return TryGetWebException(ex.InnerException, out webException);
            }

            return webException != null;
        }

        /// <summary>
        /// Determines if a retry should be attempted, based on the web-exception type.
        /// </summary>
        /// <param name="ex">The web-exception to review.</param>
        /// <returns>true if a retry should be attempted; otherwise false.</returns>
        private static bool IsRetryableWebException(WebException ex)
        {
            switch (ex.Status)
            {
                // Retry on the following
                case WebExceptionStatus.ReceiveFailure:
                case WebExceptionStatus.SendFailure:
                case WebExceptionStatus.Timeout:
                case WebExceptionStatus.ConnectFailure:
                case WebExceptionStatus.ConnectionClosed:
                case WebExceptionStatus.KeepAliveFailure:
                    return true;

                default:
                    return false;
            }
        }
    }
}
