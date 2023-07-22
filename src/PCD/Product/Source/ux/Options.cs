using System;
using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace Microsoft.PrivacyServices.UX
{
    class Options
    {
        [Option("i9nMode", HelpText = "Boolean representing if integration testing mode is active or not.")]
        public bool I9nMode { get; set; }

        public static string GetUsage()
        {
            var result = Parser.Default.ParseArguments<Options>(new string[] { "--help" });
            return HelpText.RenderUsageText(result);
        }
    }
}
