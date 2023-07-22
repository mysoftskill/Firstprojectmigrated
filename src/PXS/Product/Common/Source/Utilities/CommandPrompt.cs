// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Diagnostics;

    public static class CommandPrompt
    {
        // List of exit codes: https://msdn.microsoft.com/en-us/library/ms194959(v=vs.100).aspx
        private const int SuccessExitCode = 0;

        public static string Execute(string command, int timeoutInMilliseconds = 10000)
        {
            ProcessStartInfo commandToExecute = new ProcessStartInfo("cmd.exe", "/C " + command);
            commandToExecute.UseShellExecute = false; // required to redirect output
            commandToExecute.CreateNoWindow = true;
            commandToExecute.RedirectStandardOutput = true;

            using (Process process = Process.Start(commandToExecute))
            {
                if (!process.WaitForExit(timeoutInMilliseconds))
                {
                    process.Kill();
                    string message = "Command did not complete in time. Command: {0}, TimeoutInMilliseconds: {1}".FormatInvariant(command, timeoutInMilliseconds);
                    throw new TimeoutException(message);
                }

                string output = process.StandardOutput.ReadToEnd();
                if (process.ExitCode != SuccessExitCode)
                {
                    throw new CommandPromptRequestException(command, process.ExitCode, output);
                }

                return output;
            }
        }
    }
}
