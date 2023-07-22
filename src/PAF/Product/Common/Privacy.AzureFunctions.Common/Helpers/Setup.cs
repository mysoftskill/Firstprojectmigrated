namespace Microsoft.PrivacyServices.AzureFunctions.Common.Helpers
{
    using System;

    /// <summary>
    /// Common helper functions for PAF setup
    /// </summary>
    public static class Setup
    {
        /// <summary>
        /// Gives the Role Instance
        /// </summary>
        /// <returns> string with RoleInstance IP.</returns>
        public static string GetRoleInstance()
        {
            System.Net.IPAddress[] addresses = System.Net.Dns.GetHostAddresses(Environment.MachineName);
            string ipAddress = null;
            foreach (var addr in addresses)
            {
                if (addr.ToString() == "127.0.0.1")
                {
                    continue;
                }
                else if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipAddress = addr.ToString();
                    break;
                }
            }

            return ipAddress;
        }
    }
}
