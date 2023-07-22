namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Injects the DerivedTypeWriterConverter to add write support.
    /// </summary>
    public class DerivedTypeContractResolver : CamelCasePropertyNamesContractResolver
    {
        /// <summary>
        /// Injects the converter for objects only.
        /// </summary>
        /// <param name="objectType">The object type.</param>
        /// <returns>The modified object contract.</returns>
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);

            var converter = contract.Converter as DerivedTypeConverter;

            if (converter != null)
            {
                contract.Converter = new DerivedTypeWriterConverter(converter.NamespacePrefix);
            }

            return contract;
        }
    }
}