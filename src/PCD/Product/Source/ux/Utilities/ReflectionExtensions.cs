using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.PrivacyServices.UX.Utilities
{
    /// <summary>
    /// Extension methods for all objects based on reflection.
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Sets a property on an object using reflection.
        /// Allows you to set internal or private properties.
        /// </summary>
        /// <param name="object">The object to modify.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value to set.</param>
        public static T WithProperty<T>(this T @object, string name, object value)
        {
            Type type;
            var originalType = type = @object.GetType();
            var prop = type.GetProperty(name);

            //  If property has a setter method, simply set the value and return.
            if (prop.SetMethod != null)
            {
                prop.SetValue(@object, value);
                return @object;
            }

            //  Traverse up the parent chain until it finds a setter method.
            while (type.BaseType != null && prop != null && prop.SetMethod == null)
            {
                type = type.BaseType;
                prop = type.GetProperty(name);
            }
            if (prop != null && prop.SetMethod != null)
            {
                prop.SetValue(@object, value);
                return @object;
            }

            //  If not found, try setting the property using field based reflection.
            var fields = originalType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
            var field = fields?.FirstOrDefault(f => f.Name.StartsWith($"<{name}>"));
            if (field != null)
            {
                field.SetValue(@object, value);
                return @object;
            }

            throw new Exception($"Could not find a valid setter method for property {prop}.");
        }
    }
}
