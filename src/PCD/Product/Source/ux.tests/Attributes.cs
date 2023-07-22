using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Osgs.Core.Helpers;

namespace Microsoft.PrivacyServices.UX.Tests
{
    /// <summary>
    /// Provides access to attributes.
    /// </summary>
    public static class Attributes
    {
        /// <summary>
        /// Checks if the class has attributes from the supplied list.
        /// </summary>
        /// <typeparam name="ClassType">Class to check.</typeparam>
        /// <param name="expectedAttributes">Expected list of attributes.</param>
        public static bool ClassHas<ClassType>(params Type[] expectedAttributes)
        {
            EnsureArgument.IsNot(null == expectedAttributes || 0 == expectedAttributes.Length, nameof(expectedAttributes));

            var typeAttributes = typeof(ClassType).GetCustomAttributes(inherit: false).Select(a => a.GetType());
            return typeAttributes.Intersect(expectedAttributes).Count() == expectedAttributes.Length;
        }

        /// <summary>
        /// Checks if the class has the attribute of specified type, which satisfies predicate.
        /// </summary>
        /// <typeparam name="ClassType">Class to check.</typeparam>
        /// <typeparam name="AttributeType">Attribute to check.</typeparam>
        /// <param name="predicate">Predicate for the attribute.</param>
        public static bool ClassHas<ClassType, AttributeType>(Func<AttributeType, bool> predicate)
        {
            EnsureArgument.NotNull(predicate, nameof(predicate));

            return typeof(ClassType).GetCustomAttributes(inherit: false).Any(attribute => attribute.GetType() == typeof(AttributeType) && predicate((AttributeType)attribute));
        }
    }
}
