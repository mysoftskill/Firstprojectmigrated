// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    public class ExportStatusRecord
    {
        public static bool ParseUserId(string userIdStr, out long userId)
        {
            return long.TryParse(userIdStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out userId);
        }

        public IList<string> DataTypes { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public string ExportId { get; set; }

        public bool IsComplete { get; set; }

        public string LastError { get; set; }

        public DateTimeOffset LastSessionEnd { get; set; }

        public DateTimeOffset LastSessionStart { get; set; }

        public List<ExportDataResourceStatus> Resources { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public string Ticket { get; set; }

        public string UserId { get; set; }

        public DateTimeOffset ZipFileExpires { get; set; }

        public long ZipFileSize { get; set; }

        public Uri ZipFileUri { get; set; }

        public string[] Flights { get; set; }

        public ExportStatusRecord(string exportId)
        {
            this.ExportId = exportId;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            this.AppendNonNull(sb, "id", this.UserId);
            this.AppendNonNull(sb, "rq", this.ExportId);
            this.AppendNonNull(sb, "IsComplete", this.IsComplete.ToString());
            this.AppendNonNull(sb, "err", this.LastError);
            this.AppendNonNull(sb, "st", this.LastSessionStart.ToString());
            this.AppendNonNull(sb, "end", this.LastSessionEnd.ToString());
            this.AppendNonNull(sb, "frm", this.StartTime.ToString());
            this.AppendNonNull(sb, "to", this.EndTime.ToString());
            this.AppendNonNull(sb, "flights", string.Join(";", this.Flights ?? Enumerable.Empty<string>()));
            if (this.DataTypes != null && this.DataTypes.Any())
            {
                sb.Append(" DataTypes: ");
                foreach (string dt in this.DataTypes)
                {
                    this.AppendNonNull(sb, "nme", dt);
                }
            }
            if (this.Resources != null && this.Resources.Any())
            {
                sb.Append(" Resources: ");
                foreach (ExportDataResourceStatus res in this.Resources)
                {
                    this.AppendNonNull(sb, "nme", res.ResourceDataType);
                    this.AppendNonNull(sb, "IsComplete", res.IsComplete.ToString());
                    this.AppendNonNull(sb, "st", res.LastSessionStart.ToString());
                    this.AppendNonNull(sb, "end", res.LastSessionEnd.ToString());
                }
            }
            return sb.ToString();
        }

        private void AppendNonNull(StringBuilder sb, string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                sb.Append(label);
                sb.Append(" ");
                sb.Append(value);
                sb.Append(" ");
            }
        }
    }
}
