using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.PrivacyServices.DataManagement.Client.Factories
{
    public static class SecureXmlReaderFactory
    {
        private static readonly XmlReaderSettings DefaultSettings = new XmlReaderSettings
        {
            // Configure XmlReaderSettings for secure parsing
            DtdProcessing = DtdProcessing.Prohibit, // The DtdProcessing property is set to Prohibit to disable the processing of DTDs(document type definitions),
                                                    // which can be used to perform various kinds of XML-based attacks.
            XmlResolver = null, // The XmlResolver property is set to null to prevent the resolution of external entities, which can be used to inject malicious content into the XML document.
            MaxCharactersFromEntities = 0 // Finally, the MaxCharactersFromEntities property is set to 0 to prevent the expansion of XML entities, which can also be used to perform attacks such as denial of service.
        };

        public static XmlReader CreateReader(Stream stream, XmlReaderSettings settings = null)
        {
            // Use the provided settings, or the default settings if none are provided
            settings = settings ?? DefaultSettings;

            // Create and return the secure XmlReader using the specified stream and settings
            return XmlReader.Create(stream, settings);
        }
    }
}
