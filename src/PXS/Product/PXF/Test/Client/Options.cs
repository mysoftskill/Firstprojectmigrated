// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.TestClient
{
    using CommandLine;
    using CommandLine.Text;

    /// <summary>
    /// Options
    /// </summary>
    public class Options
    {
        public const string PostExportRequest = "PostExportRequest";

        public const string ListExportHistory = "ListExportHistory";

        public const string ExportSync = "Export";

        public const string OperationHelpText = @"Command line operation:
                                                                    " + PostExportRequest + @",
                                                                    " + ListExportHistory + @",                                                                
                                                                     " + ExportSync + "  (post, loop status until complete)" ;

        [Option('e', Default = Environment.INT, HelpText = "Target environment: DEV, INT, PPE, PROD")]
        public Environment Environment { get; set; }

        [Option('d', Default = null, HelpText = "Value allows overriding the target service-endpoint-uri.")]
        public string ServiceEndpointUri { get; set; }

        [Option('s', Default = false, HelpText = "True indicates server cert validation is skipped.")]
        public bool? SkipServerCertValidation { get; set; }

        [Option('u', Default = null, HelpText = "The username.")]
        public string UserName { get; set; }

        [Option('p', "password", Default = null, HelpText = "The password.")]
        public string Password { get; set; }

        [Option('o', "operation", Default = null, HelpText = OperationHelpText)]
        public string Operation { get; set; }

        [Option('a', "argument1", Default = null, HelpText = "argument 1 for the operation if specified")]
        public string Argument1 { get; set; }

        [Option('b', "argument2", Default = null, HelpText = "argument 2 for the operation if specified")]
        public string Argument2 { get; set; }

        [Option('c', "argument3", Default = null, HelpText = "argument 3 for the operation if specified")]
        public string Argument3 { get; set; }

        [Option('f', "argument4", Default = null, HelpText = "argument 4 for the operation if specified")]
        public string Argument4 { get; set; }

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
