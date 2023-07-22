// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyHost
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// A host startup decorator which logs the execution of the service as well as any unhandled exceptions.
    /// Logs directly to Console since Trace may have a dependency on AP Logger, which requires AP runtime to be initialized.
    /// Console logs appear in D:\data\serviceouput\{{servicename}}.{{buildlabel}}.out. 
    /// </summary>
    public class ExceptionDecorator : HostDecorator
    {
        private string[] args;

        public ExceptionDecorator(string[] args)
        {
            this.args = args;
        }

        public override ConsoleSpecialKey? Execute()
        {
            ConsoleSpecialKey? cancelKey = null;

            try
            {
                Trace.TraceInformation("[{0:o}] {3},{4} ({1}) (.Net/{2})", DateTime.UtcNow, Environment.OSVersion, Environment.Version, Environment.MachineName, Environment.CurrentDirectory);
                Trace.TraceInformation("ARGS: {0}", string.Join(" ", this.args));

                Trace.TraceInformation("[{0:o}] Entering service execution.", DateTime.UtcNow);
                cancelKey = base.Execute();
                Trace.TraceInformation("[{0:o}] Exiting cleanly (signal: {1}).", DateTime.UtcNow, cancelKey);
            }
            catch (Exception unhandledException)
            {
                Trace.TraceError("[{0:o}] Exiting dirty (signal: unhandledException).", DateTime.UtcNow);
                Trace.TraceError(unhandledException.ToString());
            }

            return cancelKey;
        }
    }
}
