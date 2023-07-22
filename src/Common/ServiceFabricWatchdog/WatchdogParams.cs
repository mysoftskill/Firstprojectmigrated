namespace WatchdogSvc
{
    using System;

    /// <summary>
    /// Contains a list of watchdog local environment params defined in ServiceManifest.xml
    /// </summary>
    public static class WatchdogParams
    {
        private static readonly string uptimeCheckEnabled = Environment.GetEnvironmentVariable("WD_UptimeCheckEnabled");

        private static readonly string probeCheckEnabled = Environment.GetEnvironmentVariable("WD_ProbeCheckEnabled");

        private static readonly string probeCheckUri = Environment.GetEnvironmentVariable("WD_ProbeCheckUri");

        public static string UptimeCheckProcessName => Environment.GetEnvironmentVariable("WD_UptimeCheckProcessName");

        public static string TargetServiceManifestName => Environment.GetEnvironmentVariable("WD_ReportServiceManifestName");

        public static bool TryParseUptimeCheckEnabled(out bool value)
        {
            value = false;
            if (bool.TryParse(uptimeCheckEnabled, out bool result))
            {
                value = result;
                return true;
            }

            return false;
        }

        public static bool TryParseProbeCheckEnabled(out bool value)
        {
            value = false;
            if (bool.TryParse(probeCheckEnabled, out bool result))
            {
                value = result;
                return true;
            }

            return false;
        }

        public static bool TryParseProbeCheckUri(out Uri value)
        {
            value = null;
            if (Uri.TryCreate(probeCheckUri, UriKind.RelativeOrAbsolute, out Uri result))
            {
                value = result;
                return true;
            }

            return false;
        }
    }
}
