// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     The sampling rate
    /// </summary>
    public class SamplingRate
    {
        /// <summary>
        ///     Max Tokens
        /// </summary>
        public long MaxTokens { get; }

        /// <summary>
        ///     Tokens Per Second
        /// </summary>
        public double TokensPerSecond { get; }

        /// <summary>
        ///     Creates a new instance of Sampling Rate
        /// </summary>
        /// <param name="tokensPerSecond"></param>
        /// <param name="maxTokens"></param>
        public SamplingRate(double tokensPerSecond, long maxTokens)
        {
            this.MaxTokens = maxTokens;
            this.TokensPerSecond = tokensPerSecond;
        }
    }

    /// <summary>
    ///     Implementation of <see cref="ISamplingManager" />
    /// </summary>
    /// <seealso cref="ISamplingManager" />
    public class SamplingManager : ISamplingManager
    {
        /// <summary>
        ///     The sampler tokens
        /// </summary>
        private readonly Dictionary<string, SamplerToken> samplerTokens;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SamplingManager" /> class.
        /// </summary>
        /// <remarks>The default constructor will not do any sampling.</remarks>
        public SamplingManager()
            : this((Dictionary<string, double>)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SamplingManager" /> class.
        /// </summary>
        /// <param name="idAndRpsMap">The identifier and RPS map.</param>
        public SamplingManager(Dictionary<string, SamplingRate> idAndRpsMap)
        {
            this.samplerTokens = idAndRpsMap?.ToDictionary(
                                     pair => pair.Key,
                                     pair => new SamplerToken(pair.Value.MaxTokens, pair.Value.TokensPerSecond, 100)) ?? new Dictionary<string, SamplerToken>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SamplingManager" /> class.
        /// </summary>
        /// <param name="idAndRpsMap">The identifier and RPS map.</param>
        public SamplingManager(Dictionary<string, double> idAndRpsMap)
        {
            this.samplerTokens = idAndRpsMap?.ToDictionary(
                                     pair => pair.Key,
                                     pair =>
                                     {
                                         long temp = (long)pair.Value;
                                         if (pair.Value > temp)
                                         {
                                             ++temp;
                                         }

                                         return new SamplerToken(temp, pair.Value, 100);
                                     }) ?? new Dictionary<string, SamplerToken>();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Applies the sampling to the action
        /// </summary>
        /// <param name="identifier">Identifier for the component that might be sampled</param>
        /// <param name="action">The action.</param>
        /// <returns>
        ///     A task
        /// </returns>
        public async Task ApplySamplingAsync(string identifier, Func<Task> action)
        {
            // Only apply sampling if we are configured for it
            if (this.samplerTokens.TryGetValue(identifier, out SamplerToken token))
            {
                // Only send if we can acquire a token
                if (token.AcquireToken())
                {
                    await action().ConfigureAwait(false);
                }
            }
            else
            {
                await action().ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Downsizes a collection depending on how many items in the collection are allowed to get through
        /// </summary>
        /// <typeparam name="TValue">The type of items in the collection</typeparam>
        /// <param name="identifier">The identifier for the component that has sampling being done</param>
        /// <param name="values">All the values trying to be processed</param>
        /// <returns>0 or more values from the initial collection of values that are allowed to go through</returns>
        public IEnumerable<TValue> ApplySamplingToCollection<TValue>(string identifier, IEnumerable<TValue> values)
        {
            return this.samplerTokens.TryGetValue(identifier, out SamplerToken token) ? values.Where(val => token.AcquireToken()) : values;
        }

        /// <summary>
        ///     Helps with limiting the amount of requests that can go through per second
        /// </summary>
        private class SamplerToken
        {
            /// <summary>
            ///     The maximum tokens we can have stored to prevent over sending
            /// </summary>
            private readonly long maxTokens;

            /// <summary>
            ///     The synchronize lock for generating tokens
            /// </summary>
            private readonly object sync = new object();

            /// <summary>
            ///     The token grant interval in milliseconds
            /// </summary>
            private readonly long tokenGrantIntervalMilliseconds;

            /// <summary>
            ///     The tokens generated every second
            /// </summary>
            private readonly double tokensPerSecond;

            /// <summary>
            ///     The available tokens
            /// </summary>
            private long availableTokens;

            /// <summary>
            ///     The last generation time of tokens
            /// </summary>
            private DateTimeOffset lastGenerationTime;

            /// <summary>
            ///     The partial tokens
            /// </summary>
            private double partialTokens;

            /// <summary>
            ///     Initializes a new instance of the <see cref="SamplerToken" /> class.
            /// </summary>
            /// <param name="maxTokens">The maximum tokens.</param>
            /// <param name="tokensPerSecond">The tokens to generate per second.</param>
            /// <param name="tokenGrantIntervalMilliseconds">The token grant interval milliseconds.</param>
            public SamplerToken(long maxTokens, double tokensPerSecond, long tokenGrantIntervalMilliseconds)
            {
                this.maxTokens = maxTokens;
                this.tokensPerSecond = tokensPerSecond;
                this.tokenGrantIntervalMilliseconds = tokenGrantIntervalMilliseconds;
                this.availableTokens = (long)tokensPerSecond;
                this.partialTokens = tokensPerSecond - this.availableTokens;
                this.lastGenerationTime = DateTimeOffset.UtcNow;
            }

            /// <summary>
            ///     Acquires a token if there are any available
            /// </summary>
            /// <returns><c>true</c> if a token was acquired, otherwise <c>false</c></returns>
            public bool AcquireToken()
            {
                this.IncrementTokens();
                double value = Interlocked.Decrement(ref this.availableTokens);
                if (value < 0)
                {
                    Interlocked.Exchange(ref this.availableTokens, 0);
                }

                return value >= 0;
            }

            /// <summary>
            ///     Determines whether this instance can generate tokens.
            /// </summary>
            /// <returns>
            ///     <c>true</c> if this instance can generate tokens; otherwise, <c>false</c>.
            /// </returns>
            private bool CanGenerateTokens()
            {
                TimeSpan time = DateTimeOffset.UtcNow - this.lastGenerationTime;
                return time.TotalMilliseconds >= this.tokenGrantIntervalMilliseconds;
            }

            /// <summary>
            ///     Increments the tokens if able
            /// </summary>
            private void IncrementTokens()
            {
                if (!this.CanGenerateTokens())
                {
                    return;
                }

                lock (this.sync)
                {
                    if (!this.CanGenerateTokens())
                    {
                        return;
                    }

                    if (Interlocked.Read(ref this.availableTokens) < 0)
                    {
                        Interlocked.Exchange(ref this.availableTokens, 0);
                    }

                    TimeSpan time = DateTimeOffset.UtcNow - this.lastGenerationTime;
                    this.lastGenerationTime = DateTimeOffset.UtcNow;
                    if (time.TotalSeconds > 1000)
                    {
                        time = TimeSpan.FromSeconds(1000);
                    }

                    double newTokens = time.TotalSeconds * this.tokensPerSecond;
                    long whole = (long)newTokens;
                    Interlocked.Add(ref this.availableTokens, whole);
                    this.partialTokens += newTokens - whole;
                    if (this.partialTokens > 1)
                    {
                        Interlocked.Increment(ref this.availableTokens);
                        --this.partialTokens;
                    }

                    if (Interlocked.Read(ref this.availableTokens) > this.maxTokens)
                    {
                        Interlocked.Exchange(ref this.availableTokens, this.maxTokens);
                    }
                }
            }
        }
    }
}
