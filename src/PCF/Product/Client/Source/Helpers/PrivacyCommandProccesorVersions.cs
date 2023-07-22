using System;

namespace Microsoft.PrivacyServices.CommandFeed.Client.Helpers
{
    public static class PrivacyCommandProccesorVersions
    {
        public static readonly Version v1 = new Version("1.0");
        public static readonly Version v2 = new Version("2.0");
        public static Version defaultVersion => v2;

    }
}
