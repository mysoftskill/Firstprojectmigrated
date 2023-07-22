using Microsoft.Azure.ComplianceServices.Common;
using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    /// <summary>
    /// The Builder Design Pattern was introduced to address the problems associated with creating complex objects 
    /// using constructors, particularly when the object has many properties.
    /// 
    /// In the <see cref="AzureActiveDirectoryProvider"/> class, for example, we need to pass the appConfig object. 
    /// One solution is to add it to the existing constructor but that can lead to a constructor with many parameters. 
    /// Also, the existing constructor is referenced from multiple classes, this can make the code harder to read and maintain, 
    /// a problem often referred to as the "telescoping constructor" anti-pattern. Adding new properties or changing the order of 
    /// properties can also require changes throughout the codebase wherever the constructor is called, potentially 
    /// introducing bugs.
    ///
    /// The Builder pattern solves these problems by providing a way to construct a complex object step by step, 
    /// where each step is represented by a method call. These methods can be called in any order, and they can 
    /// be called multiple times or not at all, providing a lot of flexibility in how the object is constructed. 
    /// This makes the code easier to read and write, and also makes it easier to add new properties without 
    /// changing existing code, making the codebase more maintainable and less error-prone.
    ///
    /// Additionally, by encapsulating the construction logic in the Builder class, we also ensure that the 
    /// <see cref="AzureActiveDirectoryProvider"/> objects are always created in a valid state, as the Builder can validate the parameters and throw exceptions 
    /// if invalid parameters are provided.
    /// </summary>
    public class AzureActiveDirectoryProviderBuilder
    {
        private IAzureActiveDirectoryProviderConfig configuration;
        private IEventWriterFactory eventWriterFactory;
        private IList<X509Certificate2> tokenDecryptionCertificates;
        private IAppConfiguration appConfiguration;

        public AzureActiveDirectoryProviderBuilder(IAzureActiveDirectoryProviderConfig configuration, IEventWriterFactory eventWriterFactory, IList<X509Certificate2> tokenDecryptionCertificates)
        {
            this.configuration = configuration;
            this.eventWriterFactory = eventWriterFactory;
            this.tokenDecryptionCertificates = tokenDecryptionCertificates;
        }
        public AzureActiveDirectoryProviderBuilder WithAppConfiguration(IAppConfiguration appConfiguration)
        {
            this.appConfiguration = appConfiguration;
            return this;
        }
        public AzureActiveDirectoryProvider Build()
        {
            var aadProvider = new AzureActiveDirectoryProvider(configuration, tokenDecryptionCertificates, eventWriterFactory, appConfiguration);
            return aadProvider;
        }
    }
}
