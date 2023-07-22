namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;

    /// <summary>
    /// Extension methods for exceptions.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Gets the exception name for instrumentation purposes.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>The name.</returns>
        public static string GetName(this Exception exception)
        {
            return exception?.GetType().FullName ?? "null";
        }
    }
}