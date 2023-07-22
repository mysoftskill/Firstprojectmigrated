
namespace Microsoft.Azure.ComplianceServices.Common
{
    using System;
    using System.IO;
    using System.Threading;
    using Microsoft.PrivacyServices.Common.Azure;
    using StackExchange.Redis;

    /// <summary>
    /// Register instance of this class as a Singleton object in DI. ConnectionMultiplexer handles connection failures and reconnection
    /// https://docs.microsoft.com/en-us/azure/azure-cache-for-redis/cache-best-practices
    /// </summary>
    public class RedisConnection : IRedisConnection
    {
        private Lazy<ConnectionMultiplexer> lazyConnection;
        private ConnectionMultiplexer ConnectionMultiplexer => lazyConnection.Value;

        /// <summary>
        /// Redis connection option https://stackexchange.github.io/StackExchange.Redis/Configuration.html
        /// </summary>
        private readonly ConfigurationOptions options;

        private readonly ILogger logger;

        // Variables used for Dispose
        private bool disposed = false;
        private readonly object disposeLock = new object();

        // Variables used for ForceReconnect
        private static long lastReconnectTicks = DateTimeOffset.MinValue.UtcTicks;
        private static DateTimeOffset firstErrorTime = DateTimeOffset.MinValue;
        private static DateTimeOffset previousErrorTime = DateTimeOffset.MinValue;

        private static readonly object reconnectLock = new object();

        // In general, let StackExchange.Redis handle most reconnects, 
        // so limit the frequency of how often this will actually reconnect.
        private readonly TimeSpan reconnectMinFrequency = TimeSpan.FromSeconds(60);

        // if errors continue for longer than the below threshold, then the 
        // multiplexer seems to not be reconnecting, so re-create the multiplexer
        private readonly TimeSpan reconnectErrorThreshold = TimeSpan.FromSeconds(30);

        public RedisConnection(string clientName, string endpoint, int port, string password, ILogger logger)
        {
            this.options = new ConfigurationOptions
            {
                Ssl = true,
                ClientName = clientName,
                EndPoints = { { endpoint, port } },
                Password = password,
                AbortOnConnectFail = false,
                ConnectRetry = 3,
                ConnectTimeout = 15000, // In ms
                SyncTimeout = 15000  // In ms
            };

            this.logger = logger;
            CreateMultiplexer();
        }

        /// <inheritdoc />
        public IDatabase GetDatabase(int dbId)
        {
            return ConnectionMultiplexer.GetDatabase(dbId);
        }

        /// <summary>
        /// Force a new ConnectionMultiplexer to be created.  
        /// NOTES: 
        ///     1. Users of the ConnectionMultiplexer MUST handle ObjectDisposedExceptions, which can now happen as a result of calling ForceReconnect()
        ///     2. Don't call ForceReconnect for Timeouts, just for RedisConnectionExceptions or SocketExceptions
        ///     3. Call this method every time you see a connection exception, the code will wait to reconnect:
        ///         a. for at least the "ReconnectErrorThreshold" time of repeated errors before actually reconnecting
        ///         b. not reconnect more frequently than configured in "ReconnectMinFrequency"
        /// </summary>    
        public void ForceReconnect()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var previousTicks = Interlocked.Read(ref lastReconnectTicks);
            var previousReconnect = new DateTimeOffset(previousTicks, TimeSpan.Zero);
            var elapsedSinceLastReconnect = utcNow - previousReconnect;

            // If mulitple threads call ForceReconnect at the same time, we only want to honor one of them.
            if (elapsedSinceLastReconnect > reconnectMinFrequency)
            {
                lock (reconnectLock)
                {
                    utcNow = DateTimeOffset.UtcNow;
                    previousTicks = Interlocked.Read(ref lastReconnectTicks);
                    previousReconnect = new DateTimeOffset(previousTicks, TimeSpan.Zero);
                    elapsedSinceLastReconnect = utcNow - previousReconnect;

                    if (firstErrorTime == DateTimeOffset.MinValue)
                    {
                        // We haven't seen an error since last reconnect, so set initial values.
                        firstErrorTime = utcNow;
                        previousErrorTime = utcNow;
                        return;
                    }

                    if (elapsedSinceLastReconnect < reconnectMinFrequency)
                    {
                        // Some other thread made it through the check and the lock, so nothing to do.
                        return;
                    }

                    var elapsedSinceFirstError = utcNow - firstErrorTime;
                    var elapsedSinceMostRecentError = utcNow - previousErrorTime;

                    var shouldReconnect =
                        elapsedSinceFirstError >= reconnectErrorThreshold   // make sure we gave the multiplexer enough time to reconnect on its own if it can
                        && elapsedSinceMostRecentError <= reconnectErrorThreshold; // make sure we aren't working on stale data (e.g. if there was a gap in errors, don't reconnect yet).

                    // Update the previousError timestamp to be now (e.g. this reconnect request)
                    previousErrorTime = utcNow;

                    if (shouldReconnect)
                    {
                        firstErrorTime = DateTimeOffset.MinValue;
                        previousErrorTime = DateTimeOffset.MinValue;

                        var oldMultiplexer = ConnectionMultiplexer;
                        CloseMultiplexer(oldMultiplexer);
                        CreateMultiplexer();
                        Interlocked.Exchange(ref lastReconnectTicks, utcNow.UtcTicks);
                    }
                }
            }
        }

        private void CreateMultiplexer()
        {
            lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                logger.Information(nameof(RedisConnection), "RedisConnectionMultiplexer.Initialize");

                using (var stringWriter = new StringWriter())
                {
                    try
                    {
                        var connectionMultiplexer = ConnectionMultiplexer.Connect(options, log: stringWriter);

                        logger.Information(nameof(RedisConnection), $"RedisConnectionMultiplexer has been initialized. Details: {stringWriter}");

                        connectionMultiplexer.IncludeDetailInExceptions = true;

                        connectionMultiplexer.InternalError += (sender, args) =>
                        {
                            logger.Warning(nameof(RedisConnection), "RedisConnectionMultiplexer InternalError event is called. " +
                                        $"args.ConnectionType:{args.ConnectionType}, " +
                                        $"args.Origin:{args.Origin}, " +
                                        $"args.Exception:{args.Exception.Message}");
                        };
                                
                        connectionMultiplexer.ConnectionFailed += (sender, args) =>
                        {
                            logger.Warning(nameof(RedisConnection), "RedisConnectionMultiplexer ConnectionFailed event is called. " +
                                        $"args.ConnectionType:{args.ConnectionType}, " +
                                        $"args.FailureType:{args.FailureType}, " +
                                        $"args.Exception:{args.Exception.Message}");
                        };
                                
                        return connectionMultiplexer;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(nameof(RedisConnection), ex, $"RedisConnectionMultiplexer initialization error. {stringWriter}");
                        throw;
                    }
                }
            });
        }

        private void CloseMultiplexer(ConnectionMultiplexer oldMultiplexer)
        {
            if (oldMultiplexer != null)
            {
                try
                {
                    oldMultiplexer.Close();
                }
                catch (Exception ex)
                {
                    // Example error condition: if accessing old.Value causes a connection attempt and that fails.
                    logger.Error(nameof(RedisConnection), ex, $"Could not close old multiplexer");
                }
            }
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
            {
                if (lazyConnection != null)
                {
                    lock (disposeLock)
                    {
                        if (lazyConnection != null)
                        {
                            try
                            {
                                this.ConnectionMultiplexer?.Dispose();
                            }
                            catch
                            {
                            }

                            lazyConnection = null;
                        }
                    }
                }
            }

            this.disposed = true;
        }
    }
}
