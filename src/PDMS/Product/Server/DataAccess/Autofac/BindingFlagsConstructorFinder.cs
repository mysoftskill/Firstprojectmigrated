namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac
{
    using System;
    using System.Linq;
    using System.Reflection;

    using global::Autofac.Core.Activators.Reflection;

    /// <summary>
    /// Finds constructors based on their binding flags.
    /// </summary>
    public class BindingFlagsConstructorFinder : IConstructorFinder
    {
        private readonly BindingFlags bindingFlags;

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingFlagsConstructorFinder" /> class.
        /// </summary>
        /// <param name="bindingFlags">Binding flags to match.</param>
        public BindingFlagsConstructorFinder(BindingFlags bindingFlags)
        {
            this.bindingFlags = bindingFlags;
        }

        /// <summary>
        /// Finds suitable constructors on the target type.
        /// </summary>
        /// <param name="targetType">Type to search for constructors.</param>
        /// <returns>Suitable constructors.</returns>
        public ConstructorInfo[] FindConstructors(Type targetType)
        {
            return targetType.FindMembers(
                    MemberTypes.Constructor,
                    BindingFlags.Instance | this.bindingFlags,
                    null,
                    null)
                .Cast<ConstructorInfo>()
                .ToArray();
        }
    }
}