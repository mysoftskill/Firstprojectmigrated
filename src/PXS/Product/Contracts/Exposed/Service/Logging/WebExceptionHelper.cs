//--------------------------------------------------------------------------------
// <copyright file="WebExceptionHelper.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Net;

    /// <summary>
    /// Miscellaneous <seealso cref="WebException"/> helper methods.
    /// </summary>
    public static class WebExceptionHelper
    {
        /// <summary>
        /// Recursively access the inner-exceptions of <paramref name="ex"/>, until a <seealso cref="WebException"/> is found.
        /// </summary>
        /// <param name="ex">The exception to parse.</param>
        /// <param name="webException">The output web-exception.</param>
        /// <returns>true on success; otherwise false.</returns>
        public static bool TryGetWebException(Exception ex, out WebException webException)
        {
            webException = ex as WebException;

            if (webException == null && ex.InnerException != null)
            {
                return TryGetWebException(ex.InnerException, out webException);
            }

            return webException != null;
        }
    }
}
