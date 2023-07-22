// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Cortana Notebook Service List V1
    /// </summary>
    public class CortanaNotebookUserFeaturesV1 : ResourceV1
    {
        /// <summary>
        /// Gets or sets the user feature list
        /// </summary>
        [JsonProperty("userfeatures")]

        public List<UserFeature> UserFeatures { get; set; }

        /// <summary>
        /// Gets or sets the error code
        /// </summary>
        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }
    }

    /// <summary>
    /// Cortana Notebook User Feature
    /// </summary>
    public class UserFeature
    {
        /// <summary>
        /// Gets or sets the user feature id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the explicit display name (user input)
        /// </summary>
        [JsonProperty("explicitDisplayName")]
        public string ExplicitDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the implicit display name (inferences)
        /// </summary>
        [JsonProperty("implicitDisplayName")]
        public string ImplicitDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the list of explicit entities (user input)
        /// </summary>
        [JsonProperty("explicitEntities")]
        public List<UserFeatureEntity> ExplicitEntities { get; set; }

        /// <summary>
        /// Gets or sets the list of implicit entities (inferences)
        /// </summary>
        [JsonProperty("implicitEntities")]
        public List<UserFeatureEntity> ImplicitEntities { get; set; }
    }

    /// <summary>
    /// Cortana Notebook User Feature Entity
    /// </summary>
    public class UserFeatureEntity
    {
        /// <summary>
        /// Gets or sets the entity id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the entity display name
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}