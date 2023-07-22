// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyHost
{
    using System;
    using System.Diagnostics;
    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    /// A host startup decorator which resets the Trace client to use the Console trace listener.
    /// The reason this is a separate decorator is so that this can run immediatly without any dependencies on 
    /// AP runtime, usually at the beginning of startup pipeline.
    /// Logs to Console appear in D:\data\serviceouput\{servicename}.{buildlabel}.out. 
    /// </summary>
    public class ResetTraceDecorator : HostDecorator
    {
        public override ConsoleSpecialKey? Execute()
        {
            Console.WriteLine("ResetTraceDecorator executing");

            ResetTraceListeners();
            AddConsoleTraceListener();

            return base.Execute();
        }

        /// <summary>
        /// Resets the trace listeners.
        /// </summary>
        public static void ResetTraceListeners()
        {
            Trace.Listeners.Clear();
            Trace.AutoFlush = true; // Set true so that logs are saved at every write
        }

        /// <summary>
        /// Adds the console trace listener.
        /// </summary>
        public static void AddConsoleTraceListener()
        {
            using (var console = new ConsoleTraceListener())
            {
                Trace.Listeners.Add("ConsoleListener", console);
            }
        }
    }
}
