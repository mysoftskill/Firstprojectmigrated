// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.LeaseId;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Azure.DataLake.Store;
    using Microsoft.PrivacyServices.Common.Cosmos;

    /// <inheritdoc />
    /// <summary>
    ///     Used for writing PUID createdAccountsInformation to cosmos
    /// </summary>
    internal class CosmosAccountCreateWriter : IAccountCreateWriter
    {
        private static readonly TimeSpan IdLeaseDuration = TimeSpan.FromSeconds(60);

        private static readonly TimeSpan IdLeaseRenewFrequency = TimeSpan.FromSeconds(30);

        private readonly ICosmosClient cosmosClient;

        private readonly IPuidMappingConfig cosmosConfig;

        private readonly IDistributedIdFactory idFactory;

        private readonly ILogger logger;

        /// <inheritdoc />
        /// <summary>
        ///     Writes a <see cref="T:Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common.AccountCreateInformation" /> to cosmos
        /// </summary>
        /// <param name="createdAccountInformation"> The <see cref="T:Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common.AccountCreateInformation" /> to write to cosmos </param>
        public async Task<AdapterResponse<AccountCreateInformation>> WriteCreatedAccountAsync(AccountCreateInformation createdAccountInformation)
        {
            AdapterResponse<IList<AccountCreateInformation>> response =
                await this.WriteCreatedAccountsAsync(new List<AccountCreateInformation> { createdAccountInformation }).ConfigureAwait(false);
            if (!response.IsSuccess)
            {
                return new AdapterResponse<AccountCreateInformation>
                {
                    Error = response.Error
                };
            }

            return new AdapterResponse<AccountCreateInformation>
            {
                Result = response.Result.FirstOrDefault()
            };
        }

        /// <inheritdoc />
        /// <summary>
        ///     Writes a collection of <see cref="T:Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common.AccountCreateInformation" />s to cosmos
        /// </summary>
        /// <param name="createdAccountsInformation"> The <see cref="T:Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common.AccountCreateInformation" />s to write to cosmos </param>
        public async Task<AdapterResponse<IList<AccountCreateInformation>>> WriteCreatedAccountsAsync(IList<AccountCreateInformation> createdAccountsInformation)
        {
            var builder = new StringBuilder();
            foreach (AccountCreateInformation mapping in createdAccountsInformation)
            {
                builder.AppendLine(mapping.GetCsvString());
            }

            string buffer = builder.ToString();

            if (string.IsNullOrEmpty(buffer))
            {
                return new AdapterResponse<IList<AccountCreateInformation>>
                {
                    Result = createdAccountsInformation
                };
            }

            IDistributedId id = await this.idFactory.AcquireIdAsync(IdLeaseDuration).ConfigureAwait(false);
            if (id == null)
            {
                return new AdapterResponse<IList<AccountCreateInformation>>
                {
                    Error = new AdapterError(AdapterErrorCode.TooManyRequests, "Could not acquire write id", 500)
                };
            }

            using (var token = new CancellationTokenSource())
            {
                // Background renew task
                Task renewTask = RenewHelperAsync(id, IdLeaseRenewFrequency, token.Token);

                try
                {
                    string streamPath = this.GetStreamPath(id.Id);
                    AdapterResponse response = await this.AppendContentsAsync(streamPath, buffer).ConfigureAwait(false);
                    if (!response.IsSuccess)
                    {
                        return new AdapterResponse<IList<AccountCreateInformation>>
                        {
                            Error = response.Error
                        };
                    }
                }
                finally
                {
                    token.Cancel();
                    await id.ReleaseAsync().ConfigureAwait(false);

                    try
                    {
                        await renewTask.ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        this.logger.Error(nameof(CosmosAccountCreateWriter), e, "Lease id renew had an exception");
                    }
                }
            }

            return new AdapterResponse<IList<AccountCreateInformation>>
            {
                Result = createdAccountsInformation
            };
        }

        internal CosmosAccountCreateWriter(ICosmosClient cosmosClient, ILogger logger, IPuidMappingConfig cosmosConfig, IDistributedIdFactory idFactory)
        {
            this.cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.cosmosConfig = cosmosConfig ?? throw new ArgumentNullException(nameof(cosmosConfig));
            this.idFactory = idFactory ?? throw new ArgumentNullException(nameof(idFactory));
        }

        /// <summary>
        ///     Creates the stream and append contents.
        /// </summary>
        /// <param name="streamPath">The stream path.</param>
        /// <param name="contents">The contents.</param>
        /// <returns>A task to wait for append to complete</returns>
        private async Task<AdapterResponse> AppendContentsAsync(string streamPath, string contents)
        {
            try
            {
                await this.cosmosClient.AppendAsync(streamPath, Encoding.UTF8.GetBytes(contents)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Check if the stream doesn't exist yet
                if (e is AdlsException exception && exception.HttpStatus == System.Net.HttpStatusCode.NotFound)
                {
                    this.logger.Warning(
                        nameof(CosmosAccountCreateWriter),
                        e,
                        "Write to cosmos failed, stream might not yet exist");

                    try
                    {
                        // Retry after trying to create the stream
                        await this.cosmosClient.CreateAsync(streamPath, TimeSpan.FromDays(7), CosmosCreateStreamMode.OpenExisting).ConfigureAwait(false);
                        await this.cosmosClient.AppendAsync(streamPath, Encoding.UTF8.GetBytes(contents)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        return new AdapterResponse
                        {
                            Error = new AdapterError(AdapterErrorCode.ResourceNotModified, ex.Message, 0)
                        };
                    }
                } 
                else
                {
                    throw e;
                }
            }

            return new AdapterResponse();
        }

        /// <summary>
        ///     Gets the path to write the cosmos stream
        /// </summary>
        /// <param name="id">sub hour id for multi write</param>
        /// <returns> A string containing the cosmos path </returns>
        private string GetStreamPath(long id)
        {
            DateTime now = DateTime.UtcNow;

            string year = now.ToString("yyyy", CultureInfo.InvariantCulture);
            string month = now.ToString("MM", CultureInfo.InvariantCulture);
            string day = now.ToString("dd", CultureInfo.InvariantCulture);
            string hour = now.ToString("HH", CultureInfo.InvariantCulture); // trigger new stream on the hour

            string path = $"{year}/{month}/{day}/{this.cosmosConfig.StreamNamePrefix}_{hour}_{id}.{this.cosmosConfig.StreamExtension.TrimStart('.')}";

            if(this.cosmosClient.ClientTechInUse() == ClientTech.Adls)
            {
                return Path.Combine(this.cosmosConfig.RootDir, path).Replace('\\', '/');
            }
            else 
            {
                return Path.Combine(this.cosmosConfig.LogPath, path).Replace('\\', '/');
            }
        }

        /// <summary>
        ///     Renews an id periodically while the write occurs
        /// </summary>
        /// <param name="id">The id to renew</param>
        /// <param name="renewFrequency">The amount of time to wait between renew calls</param>
        /// <param name="token">The cancellation token for when renews are no longer needed</param>
        /// <returns>The renew background task</returns>
        private static async Task RenewHelperAsync(IDistributedId id, TimeSpan renewFrequency, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(renewFrequency, token).ConfigureAwait(false);
                    if (!token.IsCancellationRequested)
                    {
                        await id.RenewAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Task.Delay will throw once the task has been canceled, so this is expected
                // any other exception should still get thrown
            }
        }
    }
}
