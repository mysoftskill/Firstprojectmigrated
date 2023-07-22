namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml.Linq;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Windows.Services.ConfigGen;

    /// <summary>
    /// Parses strings with oneboxsecret=true. This attribute exists to allow us to keep local secrets on the process.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class OneboxSecretParser : IValueParser
    {
        private static readonly XName OneboxSecretAttributeName = XName.Get("oneboxsecret", string.Empty);
        
        /// <summary>
        /// Returns true if we are not in an AP environment and the attirbute is set.
        /// </summary>
        public bool CanParse(string value, XElement element)
        {
            var attribute = element.Attribute(OneboxSecretAttributeName);
            if (attribute != null)
            {
                return bool.Parse(attribute.Value) && !EnvironmentInfo.IsHostedEnvironment;
            }

            return false;
        }

        /// <summary>
        /// Reads the indicated file, if present.
        /// </summary>
        public object Parse(string value, XElement element)
        {
            if (EnvironmentInfo.IsHostedEnvironment)
            {
                throw new InvalidOperationException("This is a devbox utility not intended for general use.");
            }

            if (File.Exists(element.Value))
            {
                return File.ReadAllText(element.Value);
            }

            return element.Value;
        }

        public Type TargetType
        {
            get { return typeof(string); }
        }
    }
}