// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Test.Common
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// RouteInitializer
    /// </summary>
    public static class RouteInitializer
    {
        /// <summary>
        /// Adds the routes for titan to ap.
        /// See Carbon Wiki for detailed instructions on initializing Titan machines for test execution 
        /// <![CDATA[https://microsoft.sharepoint.com/teams/osg_oss/mem/_layouts/OneNote.aspx?id=%2Fteams%2Fosg_oss%2Fmem%2FShared%20Documents%2FPlatform%20Services%2FCustomer%20Connection%2FCarbon%20Wiki%20%5BPublished%5D&wd=target%28Validation.one%7CD8208A78-C6EA-4F22-884E-4D9693370482%2FHow%20to%20Connect%20to%20AP%20Environment%20from%20Titan%20Machine%7C4BB176B1-CB7E-40D7-BFF9-2BE614DFC265%2F%29]]>
        /// </summary>
        public static void AddRouteForTitanToAP()
        {
            // 100.127.9.0 adds the route for the entire IP range 100.127.9.0 to 100.127.9.255. 
            // This is the IP range of the EAP servers we run Rolling Deployment on.
            // Reference: https://technet.microsoft.com/en-us/library/bb490991.aspx
            AddRoute(
                destination: "100.127.9.0", 
                mask: "255.255.255.0", 
                gateway: "10.206.140.1");
        }

        /// <summary>
        /// Adds the specified route.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="mask">The mask.</param>
        /// <param name="gateway">The gateway.</param>
        private static void AddRoute(string destination, string mask, string gateway)
        {
                // Add route so Titan test machine can access AP environment
                var psi = new ProcessStartInfo();
                psi.FileName = "PsExec.exe";
                psi.WorkingDirectory = Directory.GetCurrentDirectory();
                psi.RedirectStandardOutput = false;

                psi.Arguments = string.Format(CultureInfo.InvariantCulture, @"\\{0} -accepteula -h route add {1} mask {2} {3} -p", Environment.MachineName, destination, mask, gateway);
                psi.UseShellExecute = false;

                var ps = new Process();
                ps.StartInfo = psi;
                ps.Start();
                ps.WaitForExit();

                Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "The 'route add' process exited with the code: {0}", ps.ExitCode));
        }
    }
}