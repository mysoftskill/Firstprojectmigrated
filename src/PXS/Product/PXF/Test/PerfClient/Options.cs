// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.PerfClient
{
    using CommandLine;
    using CommandLine.Text;

    /// <summary>
    /// Options
    /// </summary>
    public class Options
    {
        [Option('e', Default = Environment.INT, HelpText = "Target environment: DEV, INT, PPE, PROD")]
        public Environment Environment { get; set; }

        [Option('d', Default = null, HelpText = "Value allows overriding the target service-endpoint-uri.")]
        public string ServiceEndpointUri { get; set; }

        [Option('s', Default = false, HelpText = "True indicates server cert validation is skipped.")]
        public bool? SkipServerCertValidation { get; set; }

        public static string GetUsage()
        {
            var result = Parser.Default.ParseArguments<Options>(new string[] { "--help" });
            return HelpText.RenderUsageText(result);
        }
    }

    public enum Environment
    {
        DEV,
        INT,
        PPE,
        PROD
    }
}
