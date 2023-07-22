//--------------------------------------------------------------------------------
// <copyright file="DisposableUtilities.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.ServiceModel;

    /// <summary>
    /// Miscellaneous <seealso cref="IDisposable"/> methods.
    /// </summary>
    public static class DisposableUtilities
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "REVISIT, find a better way later. For now, (1) need to attempt disposing _all_ resources. (2) can't do much if exception occurs anyway.")]
        public static void SafeDispose<T>(T resource) where T : class
        {
            if (resource == null)
            {
                return;
            }

            // Dispose ClientBase. 
            var client = resource as ClientBase<T>;
            if (client != null)
            {
                // Workaround to prevent a potential exception in case we try to dispose (close) a faulted client.
                // More info at https://msdn.microsoft.com/en-us/library/aa355056(v=vs.110).aspx
                try
                {
                    if (client.State != CommunicationState.Faulted)
                    {
                        client.Close();
                        client = null;
                    }
                }
                catch (Exception)
                {
                    // REVISIT(nnaemeka): log? nothing we can do anyway...besides log and monitor.
                }
                finally
                {
                    if (client != null)
                    {
                        client.Abort();
                        client = null;
                    }
                }

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
