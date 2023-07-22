//--------------------------------------------------------------------------------
// <copyright file="IMemberClient.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Tools.PerformanceTester
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net.Http;
    using System.Threading;

    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                int requestsPerSecond = 5;
                int durationInSeconds = 30;

                if (args.Length == 2)
                {
                    requestsPerSecond = int.Parse(args[0], CultureInfo.InvariantCulture);
                    durationInSeconds = int.Parse(args[1], CultureInfo.InvariantCulture);
                }

                Tester tester = new Tester(
                    connectionLimit: int.MaxValue,
                    idleTimeout: int.MaxValue,
                    clientTimeout: Timeout.InfiniteTimeSpan);

                tester.Execute(
                    requestsPerSecond, 
                    durationInSeconds, 
                    HttpMethod.Get, 
                    "https://pplaiste9:44300/test/response/200", 
                    headers: new Dictionary<string, string>() { { "PUID", "985160578104137" }},
                    certificateThumbprint: "EB0177DF07E29F311AE43C609516B909ABBCCD8B").Wait();
                
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine();
            }
            finally
            {
                if (Debugger.IsAttached)
                {
                    Console.WriteLine("Press [Enter] to exit");
                    Console.ReadLine();
                }
            }
        }
    }
}
