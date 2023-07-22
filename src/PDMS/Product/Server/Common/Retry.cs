namespace Microsoft.PrivacyServices.DataManagement.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Common retry logic.
    /// </summary>
    public static class Retry
    {
        /// <summary>
        /// Retry for actions.
        /// </summary>
        /// <param name="action">Action that needs to be run.</param>
        /// <param name="retryInterval">Retry interval.</param>
        /// <param name="retryCount">Number of retries.</param>
        public static void Do(
            Action action,
            TimeSpan retryInterval,
            int retryCount = 3)
        {
            Do<object>(
                () =>
                {
                    action();
                    return null;
                },
                retryInterval,
                retryCount);
        }

        /// <summary>
        /// Retry for functions.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="func">Function that needs to be run.</param>
        /// <param name="retryInterval">Retry interval.</param>
        /// <param name="retryCount">Number of retries.</param>
        /// <returns>Return value of the function.</returns>
        public static T Do<T>(
            Func<T> func,
            TimeSpan retryInterval,
            int retryCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int retry = 0; retry < retryCount; retry++)
            {
                try
                {
                    if (retry > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }

                    return func();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }
    }
}
