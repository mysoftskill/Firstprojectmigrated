// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    public class ExportExampleRecord
    {
        private static readonly Random Rand = new Random((int)DateTime.UtcNow.Ticks);

        public DateTime DateTimeData { get; }

        public DateTimeOffset DateTimeOffsetData { get; }

        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "float")]
        public double FloatData { get; }

        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "int")]
        public int IntData { get; }

        public string StringData { get; }

        public ExportExampleRecord()
        {
            var sb = new StringBuilder();
            for (int i = Rand.Next(4, 20); i >= 0; i--)
            {
                sb.Append((char)('a' + Rand.Next(26)));
            }

            this.StringData = sb.ToString();
            this.DateTimeData = DateTime.UtcNow - TimeSpan.FromMilliseconds(Rand.Next(0, 1000 * 60 * 60));
            this.DateTimeOffsetData = DateTimeOffset.UtcNow - TimeSpan.FromMilliseconds(Rand.Next(0, 1000 * 60 * 60));
            this.IntData = Rand.Next();
            this.FloatData = Rand.NextDouble();
        }
    }
}
