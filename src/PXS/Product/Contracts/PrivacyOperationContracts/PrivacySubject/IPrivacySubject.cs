// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject
{
    using Newtonsoft.Json;

    /// <summary>
    ///     Defines privacy subject.
    /// </summary>
    [JsonConverter(typeof(Json.PolymorphicJsonConverter<IPrivacySubject>))]
    public interface IPrivacySubject
    {
        /// <summary>
        ///     Validates privacy subject.
        /// </summary>
        /// <param name="useContext">
        ///     Indicates which operation privacy subject data will be used for.
        /// </param>
        void Validate(SubjectUseContext useContext);

        /// <summary>
        ///     Validates privacy subject.
        /// </summary>
        /// <param name="useContext">
        ///     Indicates which operation privacy subject data will be used for.
        /// </param>
        /// <param name="isNewRulesFlag">
        ///     Whether new set of validation rules should be used.
        /// </param>
        void Validate(SubjectUseContext export, bool useEmailOnlyManadatoryRule);
    }
}
