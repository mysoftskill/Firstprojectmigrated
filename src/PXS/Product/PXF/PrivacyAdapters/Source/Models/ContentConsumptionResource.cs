// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    using System;

    public sealed class ContentConsumptionResource : Resource
    {
        public enum ContentType
        {
            Book,

            Episode,

            Song,

            SurroundVideo,

            Video
        }

        public string AppName { get; set; }

        public string Artist { get; set; }

        public TimeSpan ConsumptionTime { get; set; }   

        public string ContainerName { get; set; }

        public Uri ContentUrl { get; set; }

        public Uri IconUrl { get; set; }

        public ContentType MediaType { get; set; }

        public string Title { get; set; }
    }
}
