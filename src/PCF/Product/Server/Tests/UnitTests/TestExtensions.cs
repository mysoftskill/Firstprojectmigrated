namespace PCF.UnitTests
{
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Policy;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Kernel;
    using SemanticComparison;
    using SemanticComparison.Fluent;
    using System;
    using System.Linq;

    public static class TestExtensions
    {
        public static Document AsDocument(this PrivacyCommand command)
        {
            return new StorageCommandSerializer().Process(command);
        }

        public static object CreateInstance(this IFixture fixture, Type type)
        {
            return new SpecimenContext(fixture).Resolve(type);
        }
    }
}
