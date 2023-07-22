// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.PerfClient
{
    using System;
    using System.Diagnostics;
    using CommandLine;
    using Microsoft.Membership.MemberServices.PrivacyExperience.PerfClient.Views;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;

    internal class Program
    {
        public static void Main(string[] args)
        {
            var commandline = Parser.Default.ParseArguments<Options>(args);
            commandline.WithParsed(options =>
            {
                try
                {
                    ViewManager.Initialize();
                    ViewManager.NavigateForwards(new PerfTestSetupView(options));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                finally
                {
                    if (Debugger.IsAttached)
                    {
                        Console.WriteLine("Press [Enter] to exit");
                        Console.ReadLine();
                    }
                }

                Console.ReadLine();
            });
        }
    }
}
