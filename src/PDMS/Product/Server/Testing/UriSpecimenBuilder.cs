namespace Microsoft.PrivacyServices.Testing
{
    using System;
    using System.Reflection;
    using Ploeh.AutoFixture.Kernel;

    /// <summary>
    /// Creates valid URIs for any property name that ends with 'Uri'.
    /// </summary>
    public class UriSpecimenBuilder : ISpecimenBuilder
    {
        /// <summary>
        /// Creates an Uri for string properties.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="context">The context.</param>
        /// <returns>The object with Uri property filled in as a Uri.</returns>
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

            if ((pi.Name.EndsWith("Uri") || pi.Name.EndsWith("Url") || pi.Name.EndsWith("Link")) &&
                 pi.PropertyType == typeof(string))
            {
                var value = context.Resolve(typeof(Uri));
                return value.ToString();
            }
            else
            {
                return new NoSpecimen();
            }
        }
    }
}