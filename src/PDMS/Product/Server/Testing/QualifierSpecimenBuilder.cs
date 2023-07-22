namespace Microsoft.PrivacyServices.Testing
{
    using System;
    using System.Reflection;
    using Microsoft.PrivacyServices.Identity;
    using Ploeh.AutoFixture.Kernel;

    /// <summary>
    /// Creates valid Ids (Guids) for any property name that ends with 'Id'.
    /// </summary>
    public class QualifierSpecimenBuilder : ISpecimenBuilder
    {
        /// <summary>
        /// Creates an Id as a guid.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>The object with Id property filled in as a guid.</returns>
        public object Create(object request, ISpecimenContext context)
        {
            var pi = request as PropertyInfo;

            if (pi == null)
            {
                return new NoSpecimen();
            }

            if (pi.PropertyType != typeof(string))
            {
                return new NoSpecimen();
            }

            if (pi.Name.EndsWith("Qualifier") && pi.PropertyType == typeof(string))
            {
                var value = context.Resolve(typeof(AssetQualifier)) as AssetQualifier;
                return value?.Value;
            }
            else
            {
                return new NoSpecimen();
            }
        }
    }
}