// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction
{

    /// <summary>
    ///     constants used by Data Actions
    /// </summary>
    public class DataActionConsts
    {
        /// <summary>
        ///     prefix for exception Data field members for details exposed by the data action runner
        /// </summary>
        public const string ExceptionDetailsPrefix = "DataActionDetails.";

        /// <summary>
        ///     suffix for exception Data field members indicating a raw response to an incident filing request
        /// </summary>
        public const string ExceptionDataIncidentRawResponse = 
            DataActionConsts.ExceptionDetailsPrefix + "IncidentFiling.RawResponse";

        public const string ExceptionDataIncidentSev =
            DataActionConsts.ExceptionDetailsPrefix + "Incident.Severity";

        public const string ExceptionDataIncidentAgent =
            DataActionConsts.ExceptionDetailsPrefix + "Incident.AgentId";

        public const string ExceptionDataIncidentTitle =
            DataActionConsts.ExceptionDetailsPrefix + "Incident.Title";

        public const string ExceptionDataIncidentEvent =
            DataActionConsts.ExceptionDetailsPrefix + "Incident.EventName";
    }
}
