// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Test.EventHubTestCommon
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.EventHubs;

    /// <summary>
    ///     Event Hub Writer
    /// </summary>
    public class EventHubWriter : IDisposable
    {
        private readonly EventHubClient eventHubClient;

        /// <summary>
        ///     Creates a new instance of <see cref="EventHubWriter" />
        /// </summary>
        /// <param name="connectionString"></param>
        public EventHubWriter(SecureString connectionString)
        {
            IntPtr bstr = Marshal.SecureStringToBSTR(connectionString);
            try
            {
                this.eventHubClient = EventHubClient.CreateFromConnectionString(Marshal.PtrToStringBSTR(bstr));
            }
            finally
            {
                Marshal.FreeBSTR(bstr);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.eventHubClient.CloseAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        ///     Send a message to EventHub. The sent EventData will land on any arbitrarily chosen EventHubs partition.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>Task</returns>
        public Task SendAsync(string message)
        {
            return this.eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)));
        }
    }
}
