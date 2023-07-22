namespace Microsoft.Membership.MemberServices.Privacy.Core.Export
{
    using System;
    using Microsoft.Azure.Storage.Blob;

    public interface ICloudBlobFactory
    {
        /// <summary>
        ///    Initializes a new instance of the Microsoft.Azure.Storage.Blob.CloudBlob class
        ///    using an absolute URI to the blob.
        /// </summary>
        /// <param name="blobAbsoluteUri">A System.Uri specifying the absolute URI to the blob.</param>
        /// <returns></returns>
        CloudBlob GetCloudBlob(Uri blobAbsoluteUri);
    }
}
