using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pxs
{
    /// <summary>
    /// Defines a demographic subject.
    /// </summary>
    public class DemographicRequest
    {
        public DemographicSubject Subject { get; set; }

        public ManualRequestMetadata Metadata { get; set; }

        public class DemographicSubject
        {
            /// <summary>
            /// Gets or sets names.
            /// </summary>
            public IEnumerable<string> Names { get; set; }

            /// <summary>
            /// Gets or sets emails.
            /// </summary>
            public IEnumerable<string> Emails { get; set; }

            /// <summary>
            /// Gets or sets phone numbers.
            /// </summary>
            public IEnumerable<string> PhoneNumbers { get; set; }

            /// <summary>
            /// Gets or sets postal address.
            /// </summary>
            public Address PostalAddress { get; set; }

            public class Address
            {
                /// <summary>
                /// Gets or sets street number variations.
                /// </summary>
                public IEnumerable<string> StreetNumbers { get; set; }

                /// <summary>
                /// Gets or sets street name variations.
                /// </summary>
                public IEnumerable<string> StreetNames { get; set; }

                /// <summary>
                /// Gets or sets unit (apartment) number variations.
                /// </summary>
                public IEnumerable<string> UnitNumbers { get; set; }

                /// <summary>
                /// Gets or sets variations of city names.
                /// </summary>
                public IEnumerable<string> Cities { get; set; }

                /// <summary>
                /// Gets or sets variations of region/state names/codes.
                /// </summary>
                public IEnumerable<string> Regions { get; set; }

                /// <summary>
                /// Gets or sets variations of postal/ZIP codes.
                /// </summary>
                public IEnumerable<string> PostalCodes { get; set; }
            }
        }
    }
}
