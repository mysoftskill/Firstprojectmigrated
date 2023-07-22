namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Defines the context of the currently running process.
    /// </summary>
    public static class ExecutionContext
    {
        /// <summary>
        /// Indicates if a restart has been requested.
        /// </summary>
        internal static bool RestartRequested { get; set; }

        /// <summary>
        /// Requests that the current process gracefully restart at the next opportunity.
        /// </summary>
        public static void RequestRestart(
            string reason, 
            [CallerMemberName] string memberName = "", 
            [CallerFilePath] string filePath = "", 
            [CallerLineNumber] int lineNumber = -1)
        {
            Logger.Instance?.RestartRequested(memberName, filePath, lineNumber, reason);
            RestartRequested = true;
        }
    }
}
