// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Helper for getting information from CDP Events
    /// </summary>
    public class CdpEvent2Helper
    {
        /// <summary>
        ///     By default, if account didn't age out, it is safe to assume the use deleted their account
        /// </summary>
        private const AccountCloseReason DefaultDeleteReason = AccountCloseReason.UserAccountClosed;

        private readonly bool canLog;

        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CdpEvent2Helper" /> class.
        /// </summary>
        /// <param name="configManager">The configuration manager.</param>
        /// <param name="logger">The logger.</param>
        public CdpEvent2Helper(IPrivacyConfigurationManager configManager, ILogger logger)
        {
            if (configManager != null)
            {
                this.canLog = configManager.AqsWorkerConfiguration.EnableExtraLogging;
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Gets the delete reason.
        /// </summary>
        /// <param name="evt">The event.</param>
        /// <returns>The <see cref="AccountCloseReason" /></returns>
        public AccountCloseReason GetDeleteReason(CDPEvent2 evt) => this.TryGetDeleteReason(evt, out AccountCloseReason reason) ? reason : DefaultDeleteReason;

        /// <summary>
        ///     Tries to the get Cid from an event.
        /// </summary>
        /// <param name="evt">The event.</param>
        /// <param name="cid">The Cid.</param>
        /// <returns><c>true</c> if the Cid could be retrieved, otherwise <c>false</c></returns>
        /// <exception cref="ArgumentException">
        ///     CDPEvent2 - evt
        ///     or
        ///     Missing CredentialName - evt
        /// </exception>
        public bool TryGetCid(CDPEvent2 evt, out long cid)
        {
            cid = 0;
            return this.TryGetCredentialNameValue(evt, "cid", out string temp) && long.TryParse(temp, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out cid);
        }

        /// <summary>
        ///     Gets the delete reason from a Delete event
        /// </summary>
        /// <param name="evt">The event to get the delete reason from</param>
        /// <param name="reason">The reason.</param>
        /// <returns><c>true</c> on success, otherwise <c>false</c></returns>
        public bool TryGetDeleteReason(CDPEvent2 evt, out AccountCloseReason reason)
        {
            if (!this.TryGetCredentialNameValue(evt, "UserDeleteReason", out string reasonStr))
            {
                this.logger.Information(nameof(CdpEvent2Helper), $"Failed to get {nameof(AccountCloseReason)}, using default");
                reason = DefaultDeleteReason;
                return true;
            }

            switch (reasonStr)
            {
                case "0":
                    reason = AccountCloseReason.UserAccountCreationFailure;
                    break;
                case "1":
                    reason = AccountCloseReason.UserAccountClosed;
                    break;
                case "2":
                    reason = AccountCloseReason.UserAccountAgedOut;
                    break;
                default:
                    reason = DefaultDeleteReason;
                    break;
            }

            return true;
        }

        /// <summary>
        ///     Gets the GDPR pre verifier.
        /// </summary>
        /// <param name="evt">The user delete event.</param>
        /// <param name="token">The token.</param>
        /// <returns>
        ///     <c>true</c> if successful, otherwise <c>false</c>
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     CDPEvent2 - evt
        ///     or
        ///     Missing CredentialName - evt
        ///     or
        ///     Event does not contain GdprPreVerifier token
        /// </exception>
        public bool TryGetGdprPreVerifierToken(CDPEvent2 evt, out string token) => this.TryGetCredentialNameValue(evt, "GdprPreVerifier", out token);

        /// <summary>
        ///     Tries to get the is suspended flag of an aged out account.
        /// </summary>
        /// <param name="evt">The evt.</param>
        /// <param name="isSuspended">if set to <c>true</c> [is suspended].</param>
        /// <returns><c>true</c> if retrieved, otherwise <c>false</c></returns>
        public bool TryGetIsSuspended(CDPEvent2 evt, out bool isSuspended)
        {
            isSuspended = false;
            return this.TryGetCredentialNameValue(evt, "Suspended", out string temp) && bool.TryParse(temp, out isSuspended);
        }

        /// <summary>
        ///     Tries to get the last login time of an aged out account
        /// </summary>
        /// <param name="evt">The evt.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns><c>true</c> if retrieved, otherwise <c>false</c></returns>
        public bool TryGetLastLogin(CDPEvent2 evt, out DateTimeOffset timestamp)
        {
            timestamp = default(DateTimeOffset);
            return this.TryGetCredentialNameValue(evt, "LastSuccessSignIn", out string temp) && DateTimeOffset.TryParse(temp, out timestamp);
        }

        /// <summary>
        ///     Parses a string of key value pairs into a dictionary
        /// </summary>
        /// <param name="data"> A string of key value pairs in the format of "Name1:Value1,Name2:Value2,Name3:Value3" </param>
        /// <returns> A dictionary of key value pairs parsed from the string </returns>
        private Dictionary<string, string> ParseKeyValuePairsString(string data)
        {
            try
            {
                return data.Split(',').Select(keyVal => keyVal.Split(new[] { ':' }, 2)).ToDictionary(key => key[0], value => value[1]);
            }
            catch (Exception e)
            {
                if (this.canLog)
                {
                    this.logger.Warning(nameof(CdpEvent2Helper), e, data);
                }

                // Try again but filter out whatever caused the exception
                return data.Split(',').Select(keyVal => keyVal.Split(new[] { ':' }, 2)).Where(arr => arr.Length == 2).ToDictionary(key => key[0], value => value[1]);
            }
        }

        /// <summary>
        ///     Tries the get credential name value.
        /// </summary>
        /// <param name="evt">The evt.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private bool TryGetCredentialNameValue(CDPEvent2 evt, string key, out string value)
        {
            if (!(evt.EventData is UserDelete))
            {
                this.logger.Error(nameof(CdpEvent2Helper), $"{nameof(CDPEvent2)} type is not a {nameof(UserDelete)} event");
                value = null;
                return false;
            }

            EventDataBaseProperty data = GetEventDataByName(evt, "CredentialName");
            if (data == null)
            {
                this.logger.Error(nameof(CdpEvent2Helper), $"Event is missing CredentialName ${nameof(EventDataBaseProperty)}");
                value = null;
                return false;
            }

            Dictionary<string, string> dataTable = this.ParseKeyValuePairsString(data.ExtendedData);

            return dataTable.TryGetValue(key, out value);
        }

        /// <summary>
        ///     Gets the event data with a specified name
        /// </summary>
        /// <param name="evt"> The event to get the data from </param>
        /// <param name="name"> The name of the event data to find </param>
        private static EventDataBaseProperty GetEventDataByName(CDPEvent2 evt, string name) =>
            evt?.EventData?.Property?.FirstOrDefault(data => string.Equals(data.Name, name));
    }
}
