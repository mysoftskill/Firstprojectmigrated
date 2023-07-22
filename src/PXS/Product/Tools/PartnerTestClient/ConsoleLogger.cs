// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>


namespace Microsoft.Membership.MemberServices.Test.PartnerTestClient
{
    using System;
    using System.Diagnostics;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.PrivacyServices.Common.Azure;
    
    internal class ConsoleLogger : ILogger
    {
        /// <summary>
        /// Emits a message to our log according to the specified trace level.
        /// </summary>
        /// <param name="traceLevel">The trace level.</param>
        /// <param name="componentName">The name of the component calling the method.</param>
        /// <param name="message">The text to log, can optionally contain parameters to format.</param>
        /// <param name="args">Optional - the arguments for the format string.</param>
        public void Log(TraceLevel traceLevel, string componentName, string message, params object[] args)
        {
            switch (traceLevel)
            {
                case TraceLevel.Error:
                    Error(componentName, message, args);
                    break;
                case TraceLevel.Warning:
                    Warning(componentName, message, args);
                    break;
                case TraceLevel.Info:
                    Information(componentName, message, args);
                    break;
                case TraceLevel.Verbose:
                    Verbose(componentName, message, args);
                    break;
            }
        }

        public void Log(IfxTracingLevel traceLevel, string componentName, string message, params object[] args)
        {
            Information(componentName, message, args);
        }

        public void Error(string componentName, string message, params object[] args)
        {
            ToConsole(componentName, ConsoleColor.Red, message, args);
        }

        public void Error(string componentName, Exception exception, string message, params object[] args)
        {
            ToConsole(componentName, ConsoleColor.Red, message, args);
        }

        public void Warning(string componentName, string message, params object[] args)
        {
            ToConsole(componentName, ConsoleColor.Yellow, message, args);
        }

        public void Warning(string componentName, Exception exception, string message, params object[] args)
        {
            ToConsole(componentName, ConsoleColor.Yellow, message, args);
        }

        public void Information(string componentName, string message, params object[] args)
        {
            ToConsole(componentName, ConsoleColor.White, message, args);
        }

        public void Information(string componentName, Exception exception, string message, params object[] args)
        {
            ToConsole(componentName, ConsoleColor.White, message, args);
        }

        public void Verbose(string componentName, string message, params object[] args)
        {
            ToConsole(componentName, ConsoleColor.White, message, args);
        }

        public void Verbose(string componentName, Exception exception, string message, params object[] args)
        {
            ToConsole(componentName, ConsoleColor.White, message, args);
        }

        private void ToConsole(string componentName, ConsoleColor color, string message, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                message = string.Format(message, args); // lgtm[cs/uncontrolled-format-string] Suppressing warning because format is controlled internally.
            }

            // Use White color as sentinal value indicating to reset to default colors.
            if (color == ConsoleColor.White)
            {
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = color;
            }

            Console.WriteLine("{0}; {1}", componentName, message);
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
