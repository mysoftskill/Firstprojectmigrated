namespace Microsoft.PrivacyServices.Testing
{
    using System;
    using System.Reflection;
    using Ploeh.AutoFixture.Kernel;

    /// <summary>
    /// Creates valid Ids (Guids) for any property name that ends with 'Id'.
    /// </summary>
    public class IdSpecimenBuilder : ISpecimenBuilder
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

            if (pi.Name.EndsWith("Id") && pi.PropertyType == typeof(string))
            {
                var value = context.Resolve(typeof(Guid));
                return value.ToString();
            }
            else
            {
                return new NoSpecimen();
            }
        }
    }
}