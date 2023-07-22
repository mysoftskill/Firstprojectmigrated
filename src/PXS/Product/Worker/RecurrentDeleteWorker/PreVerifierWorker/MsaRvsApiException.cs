// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.PreVerifierWorker
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class MsaRvsApiException : Exception
    {
        public MsaRvsApiException ()
        {
        }

        public MsaRvsApiException(string message)
            : base(message)
        {
        }

        protected MsaRvsApiException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
