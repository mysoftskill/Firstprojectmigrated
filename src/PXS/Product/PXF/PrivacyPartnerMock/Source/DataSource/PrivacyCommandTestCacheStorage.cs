// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.DataSource
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Membership.MemberServices.Common.Collections;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;

    /// <summary>
    ///     PrivacyCommandTestCacheStorage
    /// </summary>
    public class PrivacyCommandTestCacheStorage : StoreBase<CommandStatusResponse>
    {
        public static PrivacyCommandTestCacheStorage Instance { get; } = new PrivacyCommandTestCacheStorage(1, 5);

        public PrivacyCommandTestCacheStorage(int minRandomItems, int maxRandomItems)
            : base(minRandomItems, maxRandomItems)
        {
        }

        /// <summary>
        ///     Gets or generates data
        /// </summary>
        /// <param name="commandId">The command id</param>
        /// <returns>Set of data for that command id</returns>
        public CommandStatusResponse Get(Guid commandId)
        {
            var randomTestData = this.CreateRandomTestData(commandId);
            lock (this.UsersLock)
            {
                if (!this.Users.ContainsKey(((AadSubject)randomTestData.Subject).OrgIdPUID))
                {
                    this.Users[((AadSubject)randomTestData.Subject).OrgIdPUID] = new List<CommandStatusResponse> { randomTestData };
                }
            }

            lock (this.CommandIdLock)
            {
                if (!this.CommandIdDictionary.TryGetValue(commandId, out var data))
                {
                    data = this.CommandIdDictionary[commandId] = randomTestData;
                }

                return data;
            }
        }

        protected object CommandIdLock { get; } = new object();

        protected LastRecentlyUsedDictionary<Guid, CommandStatusResponse> CommandIdDictionary { get; } = new LastRecentlyUsedDictionary<Guid, CommandStatusResponse>(1000);

        protected override List<CommandStatusResponse> CreateRandomTestData()
        {
            var results = new List<CommandStatusResponse>();
            var r = new Random();
            int numResults = r.Next(this.MinItems, this.MaxItems);
            for (int i = 0; i < numResults; i++)
            {
                var result = this.CreateRandomTestData(Guid.NewGuid());
                results.Add(result);
            }

            return results;
        }

        private CommandStatusResponse CreateRandomTestData(Guid commandId)
        {
            var r = new Random();
            return new CommandStatusResponse
            {
                CommandId = commandId,
                CreatedTime = DateTimeOffset.UtcNow - new TimeSpan(r.Next(30), r.Next(24), r.Next(60), r.Next(60)),
                IsGloballyComplete = false,
                IsSyntheticCommand = true,
                FinalExportDestinationUri = new Uri($"https://www.final.export.location.microsoft.com/{Guid.NewGuid().ToString()}"),
                Subject = new AadSubject { ObjectId = Guid.NewGuid(), TenantId = Guid.NewGuid(), OrgIdPUID = r.Next() },
                Requester = "7bdb2545-6702-490d-8d07-5cc0a5376dd9" // Tenant id of the 3rd party test app
            };
        }
    }
}
