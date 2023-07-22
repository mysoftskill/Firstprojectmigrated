namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// Enum to specify how the scan response was determined from AVaaS Response
    /// </summary>
    public enum DeterminationType
    {
        /// <summary>
        /// SHA not found in AVaaS Database
        /// </summary>
        ShaNotFound,

        /// <summary>
        /// V1.Determination has determination value
        /// </summary>
        V1DeterminationValueFound,

        /// <summary>
        /// V1.StaticScanResult.MsAmPreRelRelFound has determination value
        /// </summary>
        V1StaticScanResultMsAmPreRelRelFound,

        /// <summary>
        /// EX.Feeds.VT has determination value
        /// </summary>
        EXFeedsVTFound,

        /// <summary>
        /// Scan failed on AVaaS 
        /// </summary>
        ScanFailed
    }

    /// <summary>
    /// Defines information about defender scan result
    /// </summary>
    public sealed class DefenderScanResult
    {
        /// <summary>
        /// True if Scan found a malware in the content
        /// </summary>
        public bool IsMalware { get; set; }

        /// <summary>
        /// Scan determination of the content. Example "Infected: HackTool:Win32/AutoKMS"
        /// </summary>
        public string ScanDetermination { get; set; }

        /// <summary>
        /// Indicates how scan result was determined. 
        /// </summary>
        public DeterminationType DeterminationType { get; set; }
    }
}
