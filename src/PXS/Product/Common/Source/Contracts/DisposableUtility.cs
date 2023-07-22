//--------------------------------------------------------------------------------
// <copyright file="DisposableUtility.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common
{
    using System;

    /// <summary>
    /// Miscellaneous <seealso cref="IDisposable"/> methods.
    /// </summary>
    internal static class DisposableUtility
    {
        /// <summary>
        /// Attempts to safely create a type that implements <see cref="IDisposable"/>.
        /// Basically, it calls <see cref="IDisposable.Dispose()"/> if the type fails during creation.
        /// </summary>
        /// <typeparam name="TDisposable">A type that implements <see cref="IDisposable"/>.</typeparam>
        /// <param name="newTDisposable">The create action; this is normally a call to the <typeparamref name="TDisposable"/> constructor.</param>
        /// <returns>On success, a newly created instance of <see cref="IDisposable"/>; otherwise null|any-exception.</returns>
        public static TDisposable SafeCreate<TDisposable>(Func<TDisposable> newTDisposable) where TDisposable : class, IDisposable
        {
            //// REVISIT(nnaemeka): if there's a better way (e.g. via native CLR method...), then get rid of this.

            var temp = default(TDisposable);
            var result = default(TDisposable);

            try
            {
                temp = newTDisposable();
                result = temp;

                // NOTE: upon exception, we won't get here. hence finally-block will perform proper dispose.
                temp = null;
            }
            finally
            {
                if (temp != null)
                {
                    SafeDispose<TDisposable>(temp);
                }
            }

            return result;
        }

        /// <summary>
        /// Attempts to safely dispose. 
        /// </summary>
        /// <typeparam name="T">The resource type.</typeparam>
        /// <param name="resource">The resource to disposed.</param>
        public static void SafeDispose<T>(T resource) where T : class
        {
            if (resource == null)
            {
                return;
            }

            var disposable = resource as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
