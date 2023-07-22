namespace Microsoft.PrivacyServices.DataManagement.Common.FileSystem
{
    using System;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Wrapper class to run command using process.
    /// </summary>
    public class ProcessLauncher : IProcessLauncher
    {
        /// <summary>
        /// Run specified command with arguments using process.
        /// </summary>
        /// <param name="command">The path to the command to be run.</param>
        /// <param name="arguments">Arguments for the command.</param>
        /// <param name="waitTimeMs">How long to wait (milliseconds) for the process before giving up and considering it a failure.</param>
        public void Run(string command, string arguments, int waitTimeMs = 60000)
        {
            var fileSystem = new FileSystem();
            if (!fileSystem.FileExists(command))
            {
                throw new FileNotFoundException($"Cannot find command at {command}");
            }
            
            var processStartInfo = new ProcessStartInfo();

            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.FileName = command;
            processStartInfo.Arguments = arguments;

            using (var process = Process.Start(processStartInfo))
            {
                if (!process.WaitForExit(waitTimeMs) || process.ExitCode != 0)
                {
                    string stdout = process.StandardOutput.ReadToEnd();

                    throw new InvalidOperationException($"Failed to run command \"{command}\". \n Output error is : \n {stdout}. \n Exit code: {process.ExitCode}");
                }
            }
        }
    }
}