// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.TestClient
{
    using System;
    using CommandLine;
    using Newtonsoft.Json;

    internal class Program
    {
        public static void Main(string[] args)
        {
            var commandline = Parser.Default.ParseArguments<Options>(args);
            commandline.WithParsed(options =>
            {
                try
                {
                    Console.WriteLine("options:" + JsonConvert.SerializeObject(options));
                    var testClient = new Client(options);
                    testClient.Run();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                if (string.IsNullOrWhiteSpace(options.Operation))
                {
                    Console.ReadLine();
                }
            });
        }
    }
}
