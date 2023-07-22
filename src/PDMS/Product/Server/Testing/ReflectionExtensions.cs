namespace Microsoft.PrivacyServices.Testing
{
    using System.Reflection;

    /// <summary>
    /// Extension methods for all objects based on reflection.
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Set a property on an object using reflection.
        /// Allows you to set internal or private properties.
        /// </summary>
        /// <param name="object">The object to modify.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value to set.</param>
        public static void SetProperty(this object @object, string name, object value)
        {
            @object.GetType().GetProperty(name).SetValue(@object, value);
        }
    }
}