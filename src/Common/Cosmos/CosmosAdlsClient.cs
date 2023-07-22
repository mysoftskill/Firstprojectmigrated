namespace Microsoft.PrivacyServices.Common.Cosmos
{
    using Microsoft.Azure.DataLake.Store;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class CosmosAdlsClient : ICosmosClient
    {
        private readonly AdlsClient adlsClient;

        private readonly AdlsConfig config;

        private readonly Func<AdlsConfig, Task<string>> refreshTokenCallback;

        private static readonly int MaxRetries = 1;

        // up to 4 MB can be written using Concurrentwrite apis.
        private static readonly long MaxConcurrentWriteBytes = 4 * 1024 * 1024;

        public CosmosAdlsClient(AdlsConfig config, string token,
            Func<AdlsConfig, Task<string>> refreshTokenCallback)
        {
            adlsClient = AdlsClient.CreateClient(accountFqdn: $"{config.AccountName}.{config.AccountSuffix}", token);
            adlsClient.SetToken(token);
            this.refreshTokenCallback = refreshTokenCallback;
            this.config = config;
        }

        // Used for Unit test.
        public CosmosAdlsClient(AdlsClient adlsClient)
        {
            this.adlsClient = adlsClient;
        }

        /// <inheritdoc/>
        public Task AppendAsync(string stream, byte[] data)
        {
            return RunActionWithAutoTokenRefreshOnAuthError(async () =>
            {
                if (data.Length <= MaxConcurrentWriteBytes)
                {
                    await this.adlsClient.ConcurrentAppendAsync(stream, true, data, 0, data.Length).ConfigureAwait(false);
                }
                else
                {
                    using (AdlsOutputStream appendStream = await this.adlsClient.GetAppendStreamAsync(stream).ConfigureAwait(false))
                    {
                        await appendStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                    }
                }
            });
        }

        /// <inheritdoc/>
        public Task CreateAsync(string stream, TimeSpan? expiry, CosmosCreateStreamMode mode)
        {
            return RunActionWithAutoTokenRefreshOnAuthError(async () =>
            {
                try
                {
                    using (AdlsOutputStream createStream = await this.adlsClient.CreateFileAsync(stream, ConvertToAdlsFileCreateMode(mode)).ConfigureAwait(false))
                    {
                        if (expiry.HasValue)
                        {
                            await SetLifetimeAsync(stream, expiry, false).ConfigureAwait(false);
                        }
                    }
                }
                catch(AdlsException e)
                {
                    if(e.RemoteExceptionName == "FileAlreadyExistsException")
                    {
                        switch (mode)
                        {
                            case CosmosCreateStreamMode.ThrowIfExists:
                                throw new StreamExistException("Stream already exists");
                            case CosmosCreateStreamMode.OpenExisting:
                                return;
                        }
                    }

                    throw;
                }
            });
        }

        /// <inheritdoc/>
        public Task CreateAsync(string stream)
        {
            return CreateAsync(stream, null, CosmosCreateStreamMode.CreateAlways);
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string stream, bool ignoreNotFound = false)
        {
            return RunActionWithAutoTokenRefreshOnAuthError(async () =>
            {
                await adlsClient.DeleteRecursiveAsync(stream).ConfigureAwait(false);
            }, ignoreNotFound);
        }

        /// <inheritdoc/>
        public Task<bool> DirectoryExistsAsync(string directoryPath)
        {
           return StreamExistsAsync(directoryPath); 
        }

        /// <inheritdoc/>
        public Task<ICollection<CosmosStreamInfo>> GetDirectoryInfoAsync(string directoryPath, bool ignoreNotFound = false)
        {
            return RunFuncWithAutoTokenRefreshOnAuthError(() =>
            {
                IEnumerable<DirectoryEntry> entries = adlsClient.EnumerateDirectory(directoryPath);
                return Task.FromResult((ICollection<CosmosStreamInfo>)entries.Select((x) => ConvertToCosmosStreamInfo(x)).ToList());
            }, ignoreNotFound);
        }

        /// <inheritdoc/>
        public Task<CosmosStreamInfo> GetStreamInfoAsync(string stream, bool allowIncompleteStream, bool ignoreNotFound = false)
        {
            return RunFuncWithAutoTokenRefreshOnAuthError(async () =>
            {
                return ConvertToCosmosStreamInfo(await adlsClient.GetDirectoryEntryAsync(stream).ConfigureAwait(false));
            }, ignoreNotFound);
        }

        /// <inheritdoc/>
        public Task<Stream> ReadStreamAsync(string stream, bool ignoreNotFound = false)
        {
            return RunFuncWithAutoTokenRefreshOnAuthError(async () =>
            {
                return (Stream) await adlsClient.GetReadStreamAsync(stream).ConfigureAwait(false);
            }, ignoreNotFound);
        }

        /// <inheritdoc/>
        public Task<DataInfo> ReadStreamAsync(string stream, long offset, long length, bool ignoreNotFound = false)
        {
            return RunFuncWithAutoTokenRefreshOnAuthError( async() =>
            {
                using (Stream readStream = await adlsClient.GetReadStreamAsync(stream))
                {
                    readStream.Seek(offset, SeekOrigin.Begin);
                    byte[] result = new byte[length];
                    int lengthRead = readStream.Read(result, 0, (int)length);
                    return new DataInfo(result, lengthRead);
                }
            }, ignoreNotFound);
        }

        /// <inheritdoc/>
        public Task RenameAsync(string stream, string target, bool allowOverwrite, bool ignoreNotFound)
        {
            return RunActionWithAutoTokenRefreshOnAuthError(async () => 
                await adlsClient.RenameAsync(stream, target, allowOverwrite).ConfigureAwait(false),
           ignoreNotFound);
        }

        /// <inheritdoc/>
        public Task SetLifetimeAsync(string stream, TimeSpan? lifetime, bool ignoreNotFound)
        {
            return RunActionWithAutoTokenRefreshOnAuthError(() =>
            {
                if (lifetime.HasValue)
                {
                    adlsClient.SetExpiryTime(stream, ExpiryOption.RelativeToCreationDate, (long)lifetime.Value.TotalMilliseconds);
                }
                else
                {
                    // Added to keep backward compatibiliy with VCClient apis.
                    adlsClient.SetExpiryTime(stream, ExpiryOption.NeverExpire, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                }
                return Task.CompletedTask;
            }, ignoreNotFound);
        }
        
        /// <inheritdoc/>
        public Task<bool> StreamExistsAsync(string stream)
        {
            return RunFuncWithAutoTokenRefreshOnAuthError(() =>
            {
                return Task.FromResult(adlsClient.CheckExists(stream));
            });
        }
        
        /// <inheritdoc/>
        public Task UploadAsync(byte[] data, string stream, TimeSpan expirationTime)
        {
            return RunFuncWithAutoTokenRefreshOnAuthError(async () =>
            {
                using (AdlsOutputStream outStream = await adlsClient.CreateFileAsync(stream, IfExists.Overwrite).ConfigureAwait(false))
                {
                    await SetLifetimeAsync(stream, expirationTime, true).ConfigureAwait(false);
                    await outStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                    return true;
                }
            });
        }

        /// <inheritdoc/>
        public ClientTech ClientTechInUse()
        {
            return ClientTech.Adls;
        }

        private static IfExists ConvertToAdlsFileCreateMode(CosmosCreateStreamMode mode)
        {
            switch(mode)
            {
                case CosmosCreateStreamMode.CreateAlways:
                    return IfExists.Overwrite;
                case CosmosCreateStreamMode.OpenExisting:
                case CosmosCreateStreamMode.ThrowIfExists:
                    return IfExists.Fail;
                default:
                    return IfExists.Overwrite;
            }
        }

        public static CosmosStreamInfo ConvertToCosmosStreamInfo(DirectoryEntry entry)
        {
            return new CosmosStreamInfo()
            {
                StreamName = entry.FullName, // Stream name is expected to have full path.
                IsDirectory = entry.Type == DirectoryEntryType.DIRECTORY,
                Length = entry.Length,
                CosmosPath = entry.FullName,
                CreateTime = entry.LastModifiedTime ?? DateTime.UtcNow,
                ExpireTime = entry.ExpiryTime,
                IsComplete = true, // DirectoryEntry does not contain this information.
                PublishedUpdateTime = entry.LastModifiedTime,
            };
        }

        private async Task RefreshTokenAsync()
        {
            if (refreshTokenCallback != null)
            {
                adlsClient.SetToken(await refreshTokenCallback(this.config).ConfigureAwait(false));
            }
        }

        private Task RunActionWithAutoTokenRefreshOnAuthError(Func<Task> func, bool ignoreNotFound = false)
        {
            async Task ActionCallInputMethodWithRetryAsync(Func<Task> methodToCall, int count, bool ignoreWhenNotFound = false)
            {
                int newCount = count + 1;
                try
                {
                    await methodToCall().ConfigureAwait(false);
                }
                catch (AdlsException e)
                {
                    // If Unauthorized, try after refreshing the token
                    if (e.HttpStatus == System.Net.HttpStatusCode.Unauthorized
                        && newCount <= MaxRetries)
                    {
                        await RefreshTokenAsync().ConfigureAwait(false);
                        await ActionCallInputMethodWithRetryAsync(methodToCall, newCount, ignoreWhenNotFound).ConfigureAwait(false);
                    }
                    else if (e.HttpStatus == System.Net.HttpStatusCode.NotFound
                        && ignoreWhenNotFound)
                    {
                        return;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return ActionCallInputMethodWithRetryAsync(func, 0, ignoreNotFound);
        }

        private Task<T> RunFuncWithAutoTokenRefreshOnAuthError<T>(Func<Task<T>> func, bool ignoreNotFound= false)
        {           
            async Task<T> FuncCallInputMethodWithRetryAsync(Func<Task<T>> methodToCall, int count, bool ignoreWhenNotFound = false)
            {
                int newCount = count + 1;
                try
                {
                    return await methodToCall().ConfigureAwait(false);
                }
                catch (AdlsException e)
                {
                    // If Unauthorized, try after refreshing the token
                    if (e.HttpStatus == System.Net.HttpStatusCode.Unauthorized 
                        && newCount <= MaxRetries)
                    {
                        await RefreshTokenAsync().ConfigureAwait(false);
                        return await FuncCallInputMethodWithRetryAsync(methodToCall, newCount, ignoreWhenNotFound).ConfigureAwait(false);
                    }
                    else if( e.HttpStatus == System.Net.HttpStatusCode.NotFound
                        && ignoreWhenNotFound)
                    {
                        return default;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return FuncCallInputMethodWithRetryAsync(func, 0, ignoreNotFound);
        }
    }
}
