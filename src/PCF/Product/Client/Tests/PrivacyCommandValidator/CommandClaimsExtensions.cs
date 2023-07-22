namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    /// <summary>
    /// Adds extension methods to <see cref="CommandClaims" />.
    /// </summary>
    internal static class CommandClaimsExtensions
    {
        /// <summary>
        /// Generates the list of security claims for <see cref="CommandClaims" />.
        /// </summary>
        /// <param name="claim">This claim object</param>
        /// <param name="keyId">The desired key id for signing.</param>
        /// <param name="signingAlgo">The signing algorithm family used</param>
        /// <param name="verifierVersion">The verifierVersion</param>
        /// <returns>An IEnumerable of claims</returns>
        internal static IList<Claim> GenerateClaims(this CommandClaims claim, string keyId, string signingAlgo, string verifierVersion = "2.0")
        {
            var claimsList = new List<Claim>();

            if (keyId != null)
            {
                claimsList.Add(new Claim("kid", keyId));
            }

            if (signingAlgo != null)
            {
                claimsList.Add(new Claim("algo", signingAlgo));
            }

            if (Enum.IsDefined(typeof(ValidOperation), claim.Operation))
            {
                claimsList.Add(new Claim("op", Enum.GetName(typeof(ValidOperation), claim.Operation)));
            }
            else
            {
                claimsList.Add(new Claim("op", "InvalidOperation"));
            }

            if (claim.CommandId != null)
            {
                List<string> commandIdList = Enumerable.Range(1, 5).Select(x => Guid.NewGuid().ToString()).ToList();
                commandIdList.Add(claim.CommandId);

                claimsList.Add(new Claim("rid", claim.CommandId));
                claimsList.Add(new Claim("rids", string.Join(",", commandIdList)));
            }

            claimsList.Add(new Claim("pa", claim.ProcessorApplicable.ToString()));
            claimsList.Add(new Claim("ca", claim.ControllerApplicable.ToString()));
            claimsList.Add(new Claim("ver", verifierVersion));

            if (claim.AzureBlobContainerTargetUri != null)
            {
                claimsList.Add(new Claim("azsp", claim.AzureBlobContainerTargetUri.OriginalString));
            }

            if (claim.Subject != null)
            {
                claimsList.AddRange(GenerateSubjectClaims((dynamic)claim.Subject, verifierVersion));
            }

            return claimsList;
        }

        private static IEnumerable<Claim> GenerateSubjectClaims(MsaSubject msaSubject, string verifierVersion)
        {
            var claimsList = new List<Claim>();
            if (msaSubject != null)
            {
                if (msaSubject.Anid != null)
                {
                    claimsList.Add(new Claim("anid", msaSubject.Anid));
                }

                if (msaSubject.Xuid != null)
                {
                    claimsList.Add(new Claim("xuid", msaSubject.Xuid));
                }

                claimsList.Add(new Claim("cid", msaSubject.Cid.ToString("X")));
                claimsList.Add(new Claim("puid", msaSubject.Puid.ToString("X")));
            }

            return claimsList;
        }

        private static IEnumerable<Claim> GenerateSubjectClaims(AadSubject aadSubject, string verifierVersion)
        {
            var claimsList = new List<Claim>();
            claimsList.Add(new Claim("tid", aadSubject.TenantId.ToString()));

            claimsList.Add(new Claim("oid", aadSubject.ObjectId.ToString()));

            claimsList.Add(new Claim("puid", aadSubject.OrgIdPUID.ToString("X")));
            return claimsList;
        }

        private static IEnumerable<Claim> GenerateSubjectClaims(AadSubject2 aadSubject, string verifierVersion)
        {
            var claimsList = new List<Claim>();
            claimsList.Add(new Claim("iss", "https://pxsmock.api.account.microsoft-int.com"));
            claimsList.Add(new Claim("tid", aadSubject.TenantId.ToString()));

            if (aadSubject.TenantIdType == TenantIdType.Home)
            {
                if (verifierVersion == "3.0")
                {
                    claimsList.Add(new Claim("dsr_tid_type", "home"));
                }

                claimsList.Add(new Claim("oid", aadSubject.ObjectId.ToString()));
                // Needs to be in Hex
                claimsList.Add(new Claim("puid", aadSubject.OrgIdPUID.ToString("X")));
            }
            else
            {
                if (verifierVersion == "2.0")
                {
                    throw new ArgumentException("Cannot generate 2.0 verifier for Resource tenant type");
                }

                claimsList.Add(new Claim("home_tid", aadSubject.HomeTenantId.ToString()));
                claimsList.Add(new Claim("dsr_tid_type", "resource"));
                claimsList.Add(new Claim("home_oid", aadSubject.ObjectId.ToString()));
                claimsList.Add(new Claim("home_puid", aadSubject.OrgIdPUID.ToString("X")));
            }
            return claimsList;
        }

        private static IEnumerable<Claim> GenerateSubjectClaims(DeviceSubject deviceSubject, string verifierVersion)
        {
            var claimsList = new List<Claim>();

            claimsList.Add(new Claim("puid", deviceSubject.GlobalDeviceId.ToString("X")));

            return claimsList;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "subject")]
        private static IEnumerable<Claim> GenerateSubjectClaims(IPrivacySubject subject)
        {
            return Enumerable.Empty<Claim>();
        }
    }
}
