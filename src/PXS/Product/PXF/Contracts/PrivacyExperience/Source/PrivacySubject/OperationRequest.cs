// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    ///     Base class for privacy subject operation requests.
    /// </summary>
    public abstract class OperationRequestBase
    {
        /// <summary>
        ///     Gets or sets privacy subject.
        /// </summary>
        [JsonProperty(Required =  Required.AllowNull)]
        public IPrivacySubject Subject { get; set; }

        /// <summary>
        ///     Gets or sets operation context. This value is optional.
        /// </summary>
        public string Context { get; set; }
    }

    /// <summary>
    ///     Privacy subject delete operation request.
    /// </summary>
    public class DeleteOperationRequest : OperationRequestBase
    {
    }

    /// <summary>
    ///     Privacy subject export operation request.
    /// </summary>
    public class ExportOperationRequest : OperationRequestBase
    {
        /// <summary>
        ///     Gets or sets storage location URI. This value is optional.
        /// </summary>
        public Uri StorageLocationUri { get; set; }
    }

    /// <summary>
    ///     Privacy subject export operation request.
    /// </summary>
    public class TestMsaCloseRequest : OperationRequestBase
    {
    }
}
