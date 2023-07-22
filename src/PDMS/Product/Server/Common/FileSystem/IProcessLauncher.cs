namespace Microsoft.PrivacyServices.DataManagement.Common.FileSystem
{
    /// <summary>
    /// An interface that defines methods for running command using process.
    /// </summary>
    public interface IProcessLauncher
    {
        /// <summary>
        /// Run specified command with arguments using process.
        /// </summary>
        /// <param name="command">The path to the command to be run.</param>
        /// <param name="arguments">Arguments for the command.</param>
        /// <param name="waitTimeMs">How long to wait (milliseconds) for the process before giving up and considering it a failure.</param>
        void Run(string command, string arguments, int waitTimeMs = 60000);
    }
}