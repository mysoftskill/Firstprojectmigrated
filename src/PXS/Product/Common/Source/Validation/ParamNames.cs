//--------------------------------------------------------------------------------
// <copyright file="ParamNames.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.Validation
{
    /// <summary>
    /// Parameter names.
    /// </summary>
    //// REVISIT(nnaemeak): this might not scale well.
    public static class ParamNames
    {
        /// <summary>
        /// The partner id.
        /// </summary>
        public const string PartnerId = "PartnerId";

        /// <summary>
        /// The subscription id.
        /// </summary>
        public const string SubscriptionId = "SubscriptionId";

        /// <summary>
        /// Get-machine-information request.
        /// </summary>
        public const string GetMachineInfoRequest = "GetMachineInfoRequest";

        /// <summary>
        /// The environment name.
        /// </summary>
        public const string EnvironmentName = "EnvironmentName";

        /// <summary>
        /// The machine function.
        /// </summary>
        public const string MachineFunction = "MachineFunction";

        /// <summary>
        /// Update-machine-property reques.t
        /// </summary>
        public const string UpdateMachinePropertyRequest = "UpdateMachinePropertyRequest";

        /// <summary>
        /// The watchdog results.
        /// </summary>
        public const string WatchdogResults = "WatchdogResults";

        /// <summary>
        /// The watchdog result.
        /// </summary>
        public const string WatchdogResult = "WatchdogResult";

        /// <summary>
        /// The watchdog property.
        /// </summary>
        public const string WatchdogProperty = "WatchdogProperty";

        /// <summary>
        /// The machine name.
        /// </summary>
        public const string MachineName = "MachineName";

        /// <summary>
        /// The payment-instrument.
        /// </summary>
        public const string PaymentInstrumentId = "PaymentInstrumentId";

        /// <summary>
        /// The shipping addresss.
        /// </summary>
        public const string ShippingAddressId = "ShippingAddressId";

        /// <summary>
        /// The subscription co-brand(?)
        /// </summary>
        public const string SubscriptionCobrand = "SubscriptionCobrand";

        /// <summary>
        /// The account id.
        /// </summary>
        public const string AccountId = "AccountId";
    }
}
