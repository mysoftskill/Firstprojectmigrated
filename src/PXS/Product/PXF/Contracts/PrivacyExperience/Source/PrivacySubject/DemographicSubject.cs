// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Defines generic demographic subject.
    ///     https://microsoft.sharepoint.com/:w:/r/teams/ngphome/ngpx/execution/_layouts/15/Doc.aspx?sourcedoc=%7B044c0763-aaf4-4add-822b-3be4167e4400%7D&amp;action=edit
    /// </summary>
    [Json.AllowSerialization]
    public class DemographicSubject : IPrivacySubject
    {
        /// <summary>
        ///     Gets or sets personal name variations.
        /// </summary>
        public IList<string> Names { get; set; }

        /// <summary>
        ///     Gets or sets email variations.
        /// </summary>
        public IList<string> Emails { get; set; }

        /// <summary>
        ///     Gets or sets phone number variations.
        /// </summary>
        public IList<string> Phones { get; set; }

        /// <summary>
        ///     Gets or sets postal address variations.
        /// </summary>
        public Address PostalAddress { get; set; }

        /// <inheritdoc cref="IPrivacySubject.Validate"/>.
        public void Validate(SubjectUseContext useContext, Boolean useEmailOnlyManadatoryRule = false)
        {
            if(useEmailOnlyManadatoryRule)
            {
                if (InvalidEmailList(this.Emails))
                    throw new PrivacySubjectInvalidException(nameof(this.Emails));
                if (InvalidPhoneNumberList(this.Phones))
                    throw new PrivacySubjectInvalidException(nameof(this.Phones));
                if (this.PostalAddress != null)
                    this.PostalAddress.Validate(useContext);
            }
            else
            {
                var hasPostalAddress = this.PostalAddress != null;

                if (InvalidList(this.Names)
                    && InvalidList(this.Emails)
                    && InvalidList(this.Phones))
                {
                    throw new PrivacySubjectIncompleteException();
                }

                if (InvalidList(this.Emails)
                    && InvalidList(this.Phones)
                    && !hasPostalAddress)
                {
                    throw new PrivacySubjectIncompleteException();
                }


                if (useContext == SubjectUseContext.Export)
                {
                    var allowedForExport = new bool[]
                    {
                    ValidList(this.Emails) && ValidList(this.Phones),
                    ValidList(this.Emails) && hasPostalAddress,
                    ValidList(this.Names) && ValidList(this.Phones) && hasPostalAddress
                    };

                    if (!allowedForExport.Any(predicate => predicate == true))
                        throw new PrivacySubjectIncompleteException($"Not enough data for using this subject in {useContext} context.");
                }
            }
        }

        private static bool ValidList(IList<string> list)
        {
            return list != null && list.Any();
        }

        private static bool InvalidList(IList<string> list)
        {
            return !ValidList(list);
        }

        /// <summary>
        /// Basic/Simple/Non-exhaustive email validation.
        /// </summary>
        private static bool InvalidEmailList(IList<string> emails)
        {
            if (emails == null)
                return true;
            return !emails.All(email =>
            {
                var emailFragments = email.Split('@');
                if (emailFragments.Length != 2 || emailFragments[0].Length == 0 || emailFragments[1].Length == 0)
                    return false;

                var emailDomainFragments = emailFragments[1].Split('.');
                return emailFragments.Length > 1 && emailDomainFragments.All(emailDomainFragment =>
                {
                    return emailDomainFragment.Length > 0;
                });
            });
        }

        private static readonly Regex SimpleE164Regex = new Regex(@"^\+?[\d]{1,15}$", RegexOptions.Compiled);

        /// <summary>
        /// Basic/Simple/Non-exhaustive e.164 phone number validation.
        /// </summary>
        private static bool InvalidPhoneNumberList(IList<string> phones)
        {
            if(phones != null)
            {
                return !phones.All(phoneNumber =>
                {
                    return SimpleE164Regex.IsMatch(phoneNumber.Replace(" ", ""));
                });
            }

            return false;
        }

        void IPrivacySubject.Validate(SubjectUseContext useContext)
        {
            
        }

        /// <summary>
        ///     Contains variations of address fields.
        /// </summary>
        public class Address
        {
            /// <summary>
            ///     Gets or sets street number variations.
            /// </summary>
            public IList<string> StreetNumbers { get; set; }

            /// <summary>
            ///     Gets or sets street name variations.
            /// </summary>
            public IList<string> StreetNames { get; set; }

            /// <summary>
            ///     Gets or sets unit (apartment) number variations.
            /// </summary>
            public IList<string> UnitNumbers { get; set; }

            /// <summary>
            ///     Gets or sets variations of city names.
            /// </summary>
            public IList<string> Cities { get; set; }

            /// <summary>
            ///     Gets or sets variations of region/state names/codes.
            /// </summary>
            public IList<string> Regions { get; set; }

            /// <summary>
            ///     Gets or sets variations of postal/ZIP codes.
            /// </summary>
            public IList<string> PostalCodes { get; set; }

            /// <summary>
            ///     Validates postal address record.
            /// </summary>
            internal void Validate(SubjectUseContext useContext)
            {
                if (InvalidList(this.StreetNames))
                    throw new PrivacySubjectInvalidException(nameof(this.StreetNames));
                if (InvalidList(this.Cities))
                    throw new PrivacySubjectInvalidException(nameof(this.Cities));
            }
        }
    }
}
