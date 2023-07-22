using System.Collections.Generic;

namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    public class CommandProperty
    {
        /// <summary>
        /// Name of Property
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// Values for a given property
        /// </summary>
        public IList<string> Values { get; set; }
    }
}