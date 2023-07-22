namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;

    /// <summary>
    /// Additional helper methods for the IHttpResult type.
    /// </summary>
    public static class IHttpResultModule
    {
        /// <summary>
        /// Get the result or throw an exception if the service call failed.
        /// </summary>
        /// <param name="result">The result object.</param>
        /// <param name="version">The error contract version.</param>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <exception cref="CallerError">
        /// Thrown for issues due to user error.
        /// </exception>
        /// <returns>The result.</returns>
        internal static IHttpResult Get(this IHttpResult result, int version)
        {
            if ((int)result.HttpStatusCode < 400)
            {
                return result;
            }
            else if ((int)result.HttpStatusCode < 500)
            {
                throw CallerError.Create(result, version);
            }
            else
            {
                throw new ServiceFault(result);
            }
        }

        /// <summary>
        /// Get the result or throw an exception if the service call failed.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="result">The result object.</param>
        /// <param name="version">The error contract version.</param>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <exception cref="CallerError">
        /// Thrown for issues due to user error.
        /// </exception>
        /// <returns>The result.</returns>
        public static IHttpResult<T> Get<T>(this IHttpResult<T> result, int version)
        {
            return Get((IHttpResult)result, version) as IHttpResult<T>;
        }

        /// <summary>
        /// Convert this result into a result of another type.
        /// </summary>
        /// <typeparam name="TOriginal">The original result type.</typeparam>
        /// <typeparam name="TNew">The new result type.</typeparam>
        /// <param name="result">The original result object.</param>
        /// <param name="converter">A function to convert the object.</param>
        /// <param name="version">The error contract version.</param>
        /// <returns>The converted object as an <see cref="IHttpResult{TNew}" />.</returns>
        internal static IHttpResult<TNew> Convert<TOriginal, TNew>(this IHttpResult<TOriginal> result, Func<IHttpResult<TOriginal>, TNew> converter, int version)
        {
            return (new HttpResult<TNew>(result, converter.Invoke(result))).Get(version);
        }
    }
}