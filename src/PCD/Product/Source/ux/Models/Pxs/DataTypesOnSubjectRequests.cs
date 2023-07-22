using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pxs
{
    public class DataTypesOnSubjectRequests
    {
        /// <summary>
        /// Gets or sets the data types supported for a demographic subject.
        /// </summary>
        public IEnumerable<string> DemographicSubject { get; set; }

        /// <summary>
        /// Gets or sets the data types supported for a Microsoft employee subject.
        /// </summary>
        public IEnumerable<string> MicrosoftEmployeeSubject { get; set; }

        /// <summary>
        /// Gets or sets the data types supported for a MSA self auth subject.
        /// </summary>
        public IEnumerable<string> MsaSelfAuthSubject { get; set; }
    }
}
