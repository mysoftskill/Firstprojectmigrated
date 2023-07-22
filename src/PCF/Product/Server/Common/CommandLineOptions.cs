namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using MS.Msn.Runtime;

    /// <summary>
    /// Defines the base class for command line options. Individual services can extend this to add their own options.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CommandLineOptions
    {
        [CommandLineParameter(Name = "help", HasValue = false, ShortName = "?")]
        private bool help = false;

        [CommandLineParameter(Name = "delay", HasValue = true)]
        private int delayStartSec = 0;

        [CommandLineParameter(Name = "debug", HasValue = false)] 
        private bool debug = false;

        /// <summary>
        /// Should we show help text?
        /// </summary>
        public bool Help
        {
            get { return this.help; }
        }

        /// <summary>
        /// Delays the service from starting for X seconds.
        /// </summary>
        public int DelayStartSec
        {
            get { return this.delayStartSec; }
        }

        /// <summary>
        /// Inserts a breakpoint before the app spins up.
        /// </summary>
        public bool Debug
        {
            get { return this.debug; }
        }

        /// <summary>
        /// Prints the usage.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public void PrintUsage(string invalidParam)
        {
            if (!string.IsNullOrEmpty(invalidParam))
            {
                Console.WriteLine("\n**ERROR: invalid parameter - " + invalidParam + "\n");
            }

            Console.WriteLine("Usage: application_name options");
            Console.WriteLine("options:");
            Console.WriteLine("\t/delay:<secs> - delays starting the application for the specified number of seconds. Default is 0.");
            Console.WriteLine("\t/debug - Invokes the debugger as soon as the application is started.");
        }
    }
}
