namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Newtonsoft.Json;

    [JsonObject]
    internal class QueueStatsResponse : IEnumerable<QueueStats>
    {
        [JsonProperty("queueStats")]
        public List<QueueStats> QueueStats { get; set; } = new List<QueueStats>();

        [JsonProperty("totalPending")]
        public long Count
        {
            get
            {
                return this.QueueStats.Sum(q => q.PendingCommandCount);
            }
        }

        public IEnumerator<QueueStats> GetEnumerator()
        {
            if (this.QueueStats != null)
            {
                foreach (var stats in this.QueueStats)
                {
                    yield return stats;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}