namespace Microsoft.Membership.MemberServices.Privacy.Core.Export
{
    using System;
    using Microsoft.Azure.Storage.Blob;

    public class CloudBlobFactory : ICloudBlobFactory
    {
        public CloudBlob GetCloudBlob(Uri blobAbsoluteUri)
        {
            return new CloudBlob(blobAbsoluteUri);
        }
    }
}
