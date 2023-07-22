// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyHost
{
    using System;
    using System.Diagnostics;

    /// <summary>
    ///     RemoveConsoleLoggerDecorator
    /// </summary>
    public class RemoveConsoleLoggerDecorator : HostDecorator
    {
        private readonly bool enableConsoleLogging;

        /// <summary>
        ///     Removes the console trace listener.
        /// </summary>
        public static void RemoveConsoleTraceListener()
        {
            // At the end of service start up, remove console logging to prevent unbounded growth of the serviceoutput log file
            Trace.Listeners.Remove("ConsoleListener");
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RemoveConsoleLoggerDecorator" /> class.
        /// </summary>
        /// <param name="enableConsoleLogging">if set to <c>true</c>, enable console logging.</param>
        public RemoveConsoleLoggerDecorator(bool enableConsoleLogging = false)
        {
            this.enableConsoleLogging = enableConsoleLogging;
        }

        public override ConsoleSpecialKey? Execute()
        {
            // allow service configuration to disable this (useful in debugging/dev box scenarios)
            if (!this.enableConsoleLogging)
            {
                RemoveConsoleTraceListener();
            }

            return base.Execute();
        }
    }
}
