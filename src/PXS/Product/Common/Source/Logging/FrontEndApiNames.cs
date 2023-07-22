//--------------------------------------------------------------------------------
// <copyright file="dddd.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.Logging
{
    /// <summary>
    /// By listing an API name in this file, IncomingApiEventWrapper will create and store a LogicalOperationFactory
    /// whenever the class is first accessed.
    /// </summary>
    public static class FrontEndApiNames
    {
        // GetServiceDetail APIs
        public const string GetServiceDetail = "GetServiceDetail";
        public const string GetServiceDetailBingOffers = "GetServiceDetailBingOffers";
        public const string GetServiceDetailBingRewards = "GetServiceDetailBingRewards";
        public const string GetServiceDetailOffice = "GetServiceDetailOffice";
        public const string GetServiceDetailOneDrive = "GetServiceDetailOneDrive";
        public const string GetServiceDetailXboxMusic = "GetServiceDetailXboxMusic";

        // Commerce Platform APIs
        public const string GetSubscriptions = "GetSubscriptions";
        public const string GetSubscriptionHistory = "GetSubscriptionHistory";
        public const string GetSubscriptionChangeHistory = "GetSubscriptionChangeHistory";
        public const string ToggleAutoRenew = "ToggleAutoRenew";
        public const string ConvertSubscription = "ConvertSubscription";
        public const string CancelSubscription = "CancelSubscription";
        public const string GetRefundAmount = "GetRefundAmount";
        public const string GetOfferInfo = "GetOfferInfo";
        public const string GetSubscriptionConvertPaths = "GetSubscriptionConvertPaths";
        public const string GetSubscriptionRenewPaths = "GetSubscriptionRenewPaths";
        public const string RenewSubscription = "RenewSubscription";
        public const string UpdateSubscription = "UpdateSubscription";
        public const string SettleBalance = "SettleBalance";
        public const string CloseBalance = "CloseBalance";

        // Payment Instrument APIs
        public const string GetPaymentMethods = "GetPaymentMethods";
        public const string GetPaymentInstruments = "GetPaymentInstruments";
        public const string RemovePaymentInstrument = "RemovePaymentInstrument";
        public const string SwitchPaymentInstruments = "SwitchPaymentInstruments";
        public const string TransferBalance = "TransferBalance";
        public const string RetrieveDocument = "RetrieveDocument";

        // Profile APIs
        public const string GetUserProfile = "GetUserProfile";
        public const string GetAddresses = "GetAddresses";
        public const string GetPublicUserProfile = "GetPublicUserProfile";
        public const string GetProfilePrimaryEmail = "GetProfilePrimaryEmail";
        public const string PutProfilePrimaryEmail = "PutProfilePrimaryEmail";
        public const string GetProfileCountry = "GetProfileCountry";
        public const string SetUserProfile = "SetUserProfile";
        public const string UploadProfilePicture = "UploadProfilePicture";
        public const string UpdateProfilePicture = "UpdateProfilePicture";
        public const string DeleteProfilePicture = "DeleteProfilePicture";
        public const string SetProfileDisplayName = "SetProfileDisplayName";
        public const string SetDefaultAddress = "SetDefaultAddress";
        public const string GetProfileAddress = "GetProfileAddress";
        public const string CreateProfileAddress = "CreateProfileAddress";
        public const string UpdateProfileAddress = "UpdateProfileAddress";
        public const string DeleteProfileAddress = "DeleteProfileAddress";
        public const string DeleteProfilePhone = "DeleteProfilePhone";

        // Order APIs
        public const string GetOrders = "GetOrders";
        public const string UpdateOrder = "UpdateOrder";
        public const string FullRefundOrder = "FullRefundOrder";
        public const string ReturnLineItems = "ReturnLineItems";
        public const string RefundLineItems = "RefundLineItems";
        public const string CancelLineItems = "CancelLineItems";
        public const string GetRiskTransactions = "GetRiskTransactions";
        public const string GetActivePurchases = "GetActivePurchases";
        public const string GetRmaDetails = "GetRmaDetails";

        // Pacific APIs
        public const string PacificControllerGetUserStatus = "GetUserStatus";
        public const string PacificControllerGetManifest = "GetManifest";

        // Recurrence APIs
        public const string GetRecurrences = "GetRecurrences";
        public const string GetRecurrence = "GetRecurrence";
        public const string UpdateRecurrence = "UpdateRecurrence";
        public const string CancelRecurrence = "CancelRecurrence";
        public const string UpdateRecurrencePaymentInstrument = "UpdateRecurrencePaymentInstrument";
        public const string RefundRecurrence = "RefundRecurrence";
        public const string GetRecurrenceRenewPaths = "GetRecurrenceRenewPaths";

        // Other APIs
        public const string GetStoredValueBalances = "GetStoredValueBalances";
        public const string DeregisterXboxMusicTuner = "DeregisterXboxMusicTuner";
        public const string TestResponse = "TestResponse";
    }
}
