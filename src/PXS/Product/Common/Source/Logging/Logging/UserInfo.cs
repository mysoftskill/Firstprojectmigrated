// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging
{
    using System;
    using System.Globalization;

    using Microsoft.CommonSchema.Services;
    using Microsoft.Telemetry;
    using Microsoft.Telemetry.Extensions;

    /// <summary>
    ///     User Id type as documented at https://osgwiki.com/wiki/CommonSchema/user_id
    /// </summary>
    public enum UserIdType
    {
        Undefined = 0,

        /// <summary>
        ///     The user's Passport ID in decimal format, aka Live ID
        /// </summary>
        DecimalPuid = 1,

        /// <summary>
        ///     The user's Passport ID in hex format, aka Live ID. When using this enum the PUID value provided will be converted and stored in Decimal format
        /// </summary>
        HexPuid = 2,

        /// <summary>
        ///     A Microsoft User ID, aka MUID, used for anonymous client identification for tracking visitors(sometimes called VisitorId) to various microsoft ".com" websites. Provisioned and
        ///     collected by ClickSteam tracking libraries such as Webi, WEDCS and other Bing tracking systems.
        /// </summary>
        Muid = 3,

        /// <summary>
        ///     An Azure AD ID
        /// </summary>
        AzureAdId = 4,

        /// <summary>
        ///     CID in decimal format  (CID is the result of a one-way hash of the user's PUID). When using this enum the CID value provided will be converted and stored in Hex format
        ///     For more information please visit https://microsoft.sharepoint.com/teams/PeopleServices/_layouts/15/WopiFrame2.aspx?sourcedoc={27D79506-5990-49F1-A5FE-0B698D2DA4A0}&amp;
        ///     file=Birth%20of%20the%20CID.doc&amp;action=default
        /// </summary>
        DecimalCid = 5,

        /// <summary>
        ///     CID in hex format (CID is the result of a one-way hash of the user's PUID)
        ///     For more information please visit https://microsoft.sharepoint.com/teams/PeopleServices/_layouts/15/WopiFrame2.aspx?sourcedoc={27D79506-5990-49F1-A5FE-0B698D2DA4A0}&amp;
        ///     file=Birth%20of%20the%20CID.doc&amp;action=default
        /// </summary>
        HexCid = 6,

        /// <summary>
        ///     This is a 128-bit hash of domain\user name, represented as a GUID string. "w:1BD8FC6E-98CE-E03D-B19D-BFD5A9BA712D" (In CS 2.0 it had curly braces like
        ///     "w:{6D01B684-9561-E0EA-3E45-3D245C8E83C4})"
        /// </summary>
        DomainUserGuid = 7,

        /// <summary>
        ///     Local Windows system ID
        /// </summary>
        WindowsSid = 8,

        /// <summary>
        ///     An anonymized value based on what the client would have used for a local ID.
        /// </summary>
        Deidentified = 9,

        /// <summary>
        ///     Google account ID
        /// </summary>
        GoogleAccount = 10,

        /// <summary>
        ///     A custom user ID known to an application.
        ///     For more information: https://osgwiki.com/wiki/CommonSchema/user_id
        /// </summary>
        CustomAppUserId = 11,

        /// <summary>
        ///     user ID is the logged domain/alias of the internal-Microsoft user that is only to be collected by the Microsoft 1st party application, only if the 1st party application
        ///     explicitly informs the user that they are collecting this data is logging the domain/alias acceptable. example: i: REDMOND/developer1
        ///     For more information: https://osgwiki.com/wiki/CommonSchema/user_id
        /// </summary>
        InternalAppUserId = 12,

        /// <summary>
        ///     Xbox live identity
        /// </summary>
        Xuid = 13,

        /// <summary>
        ///     MSA Anonymous ID
        /// </summary>
        Anid = 14
    }

    /// <summary>
    ///     Holds information about the user
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        ///     Formats a user id as decribed at https://osgwiki.com/wiki/CommonSchema/user_id
        /// </summary>
        /// <param name="type">The type of the id</param>
        /// <param name="id">The id without any prefix</param>
        /// <returns>The formatted id</returns>
        public static string FormatUserId(UserIdType type, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            string prefix;
            switch (type)
            {
                case UserIdType.DecimalPuid:
                    FormatHelpers.VerifyIsDecimal(id, "DecimalPuid");
                    prefix = "p";
                    break;
                case UserIdType.HexPuid:
                    FormatHelpers.IsValidHexPuid(id, "HexPuid");
                    id = FormatHelpers.ConvertHexToDecimal(id, "HexPuid").ToString(CultureInfo.InvariantCulture);
                    prefix = "p";
                    break;
                case UserIdType.Muid:
                    prefix = "t";
                    break;
                case UserIdType.AzureAdId:
                    prefix = "a";
                    break;
                case UserIdType.DecimalCid:
                    id = FormatHelpers.ConvertDecimalToHex(id, "DecimalCid").ToString(CultureInfo.InvariantCulture);
                    prefix = "m";
                    break;
                case UserIdType.HexCid:
                    FormatHelpers.IsValidHex(id, "HexCid");
                    prefix = "m";
                    break;
                case UserIdType.DomainUserGuid:
                    FormatHelpers.VerifyIsGuid(id, "DomainUserGuid");
                    prefix = "w";
                    break;
                case UserIdType.WindowsSid:
                    prefix = "s";
                    break;
                case UserIdType.Deidentified:
                    prefix = "d";
                    break;
                case UserIdType.GoogleAccount:
                    prefix = "g";
                    break;
                case UserIdType.CustomAppUserId:
                    prefix = "c";
                    break;
                case UserIdType.InternalAppUserId:
                    prefix = "i";
                    break;
                case UserIdType.Xuid:
                    FormatHelpers.VerifyIsDecimal(id, "Xuid");
                    prefix = "x";
                    break;
                case UserIdType.Anid:
                    FormatHelpers.IsValidHex(id, "Anid");
                    prefix = "n";
                    break;
                default:
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Unexpected UserIdType '{0}'",
                            type));
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", prefix, id);
        }

        public string Id { get; private set; }

        /// <summary>
        ///     Fills the provided envelope with the user information
        /// </summary>
        /// <param name="envelope">The envelope to be filled</param>
        public void FillEnvelope(Envelope envelope)
        {
            if (!string.IsNullOrWhiteSpace(this.Id))
            {
                user user = envelope.SafeUser();
                user.id = this.Id;
            }
        }

        /// <summary>
        ///     Sets the user Id
        /// </summary>
        /// <param name="type">The type of the id</param>
        /// <param name="id">The id without any prefix</param>
        public void SetId(UserIdType type, string id)
        {
            this.Id = FormatUserId(type, id);
        }
    }
}
