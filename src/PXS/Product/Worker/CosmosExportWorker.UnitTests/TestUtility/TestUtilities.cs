// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests
{
    using System.IO;
    using System.Text;

    public static class TestUtilities
    {
        public static void PopulateStreamWithString(
            string data,
            Stream stream)
        {
            byte[] byteData = Encoding.UTF8.GetBytes(data);

            stream.SetLength(0);
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(byteData, 0, byteData.Length);
            stream.Flush();
            stream.Seek(0, SeekOrigin.Begin);
        }
    }
}
