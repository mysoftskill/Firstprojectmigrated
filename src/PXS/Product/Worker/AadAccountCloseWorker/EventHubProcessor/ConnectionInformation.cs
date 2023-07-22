// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Contains information required to connect to Event Hub
    /// </summary>
    public class ConnectionInformation : IConnectionInformation
    {
        private const string EndpointKey = "Endpoint";

        private const string EntityPathKey = "EntityPath";

        private const string SharedAccessKeyKey = "SharedAccessKey";

        private const string SharedAccessKeyNameKey = "SharedAccessKeyName";

        private static readonly Lazy<Regex> ValidFilterRegex = new Lazy<Regex>(() => new Regex("[^a-z0-9-]"));

        private readonly Dictionary<string, string> internalDictionary;

        public string ConnectionString => $"{EndpointKey}={this.Endpoint};{SharedAccessKeyKey}={this.SharedAccessKey};{SharedAccessKeyNameKey}={this.SharedAccessKeyName}";

        public string Endpoint => this.internalDictionary[EndpointKey];

        public string EntityPath => this.internalDictionary[EntityPathKey];

        public string this[string key] => this.internalDictionary[key];

        public ConnectionInformation(string connectionString, string name)
        {
            this.internalDictionary = GetSecretDictionary(connectionString ?? throw new ArgumentNullException(nameof(connectionString)));
            this.Name = NormalizeName(name ?? throw new ArgumentNullException(nameof(name)));
        }

        private static string NormalizeName(string name)
        {
            return ValidFilterRegex.Value.Replace(name.ToLowerInvariant(), "");
        }

        private string SharedAccessKey => this.internalDictionary[SharedAccessKeyKey];

        private string SharedAccessKeyName => this.internalDictionary[SharedAccessKeyNameKey];

        public string Name { get; }

        private static Dictionary<string, string> GetSecretDictionary(string value) =>
            value.Split(';').Select(keyVal => keyVal.Split(new[] { '=' }, 2)).ToDictionary(key => key[0], val => val[1]);
    }
}
