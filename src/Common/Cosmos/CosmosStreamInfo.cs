namespace Microsoft.PrivacyServices.Common.Cosmos
{
    using System;

    public class CosmosStreamInfo
    {
        public string CosmosPath { get; set; }
        
        //
        // Summary:
        //     Is the stream complete.
        //
        // Returns:
        //     True: The stream is complete and ready to use. False: The sream is pending geo-replication
        //     or cross volume copy.
        public bool IsComplete { get; set; }
        
        //
        // Summary:
        //     Length of the stream.
        public long Length { get; set; }

        //
        // Summary:
        //     Time when the stream was updated last.
        public DateTime? PublishedUpdateTime { get; set; }

        //
        // Summary:
        //     The time when the stream will expire.
        public DateTime? ExpireTime { get; set; }

        //
        // Summary:
        //     The time when the stream was created.
        public DateTime CreateTime { get; set; }

        //
        // Summary:
        //     This shows if the stream is a file or a directory. If the value of IsDirectory
        //     is true, then the stream is a directory.
        public bool IsDirectory { get; set; }

        //
        // Summary:
        //     Name of the stream.
        public string StreamName { get; set; }
    }
}
