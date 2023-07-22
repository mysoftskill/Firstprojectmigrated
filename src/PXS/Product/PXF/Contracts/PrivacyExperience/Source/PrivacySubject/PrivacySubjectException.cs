// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class PrivacySubjectException : Exception
    {
        public PrivacySubjectException()
        {
        }

        public PrivacySubjectException(string message)
            : base(message)
        {
        }

        protected PrivacySubjectException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class PrivacySubjectIncompleteException : PrivacySubjectException
    {
        public PrivacySubjectIncompleteException()
        {
        }

        public PrivacySubjectIncompleteException(string message)
            : base(message)
        {
        }

        protected PrivacySubjectIncompleteException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class PrivacySubjectInvalidException : PrivacySubjectException
    {
        /// <summary>
        ///     Gets property name which failed validation.
        /// </summary>
        public string PropertyName { get; }

        public PrivacySubjectInvalidException(string property)
            : base($"{property} has invalid value.")
        {
            this.PropertyName = property;
        }

        protected PrivacySubjectInvalidException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
