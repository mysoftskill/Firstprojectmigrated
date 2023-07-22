// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject
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

        void Validate(SubjectUseContext delete, bool useEmailOnlyManadatoryRule);
    }
}
