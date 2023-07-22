// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    /// <summary>
    /// RequestHeaderConstants
    /// </summary>
    public static class RequestHeaderConstants
    {
        public const string PdpDiagOutput = "X-PDP-DiagOutput";
        public const string PdpOptions = "X-PDP-Options";
        public const string PdpTraceId = "X-PDP-TraceId";
        public const string PdpDiagnosticLevel = "X-PDP-DiagnosticLevel";
        public const string TestUserHeaderValue = "OSPdp.SetUserKeyTypeToTestId=true";
        public const string PartnerNameHeader = "X-REL-PARTNER";
        public const string PartnerSignatureHeader = "X-REL-PARTNER-SIG";
        public const string PartnerName = "PXS";
    }
}