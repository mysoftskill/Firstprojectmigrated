namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System;

    /// <summary>
    /// An attribute to define the derived types of an abstract class.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class DerivedTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DerivedTypeAttribute" /> class.
        /// </summary>
        /// <param name="derivedType">The derived type.</param>
        public DerivedTypeAttribute(Type derivedType)
        {
            this.DerivedType = derivedType;
        }

        /// <summary>
        /// Gets or sets the derived type.
        /// </summary>
        public Type DerivedType { get; set; }
    }
}