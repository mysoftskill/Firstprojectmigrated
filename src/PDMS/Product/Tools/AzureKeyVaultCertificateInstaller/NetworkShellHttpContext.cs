namespace Microsoft.PrivacyServices.DataManagement.AzureKeyVaultCertificateInstaller
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     A wrapper around netsh.exe http context
    ///     https://technet.microsoft.com/en-us/library/cc754516(v=ws.10).aspx
    ///     https://msdn.microsoft.com/en-us/library/windows/desktop/cc307236(v=vs.85).aspx
    ///     Note: netsh is preferred over HttpCfg.exe tool for Windows Vista and above
    ///     https://msdn.microsoft.com/en-us/library/ms733791(v=vs.110).aspx
    /// </summary>
    public static class NetworkShellHttpContext
    {
        /// <summary>
        ///     A wildcard for any IPv4 address. Use when attaching an SSL certificate to a specific port on any IP.
        ///     https://technet.microsoft.com/en-us/library/cc725882(v=ws.10).aspx#BKMK_1
        /// </summary>
        public const string IPv4Wildcard = "0.0.0.0";

        /// <summary>
        ///     Adds a new SSL server certificate binding and corresponding client certificate policies for an IP address and port.
        /// </summary>
        /// <param name="ipAddress">Specifies the IP address portion of IpPort for the binding.</param>
        /// <param name="port">Specifies the port portion of IpPort for the binding.</param>
        /// <param name="appId">Specifies the GUID to identify the owning application. Can be found in Assembly Info of your application.</param>
        /// <param name="certHash">
        ///     Specifies the SHA hash of the certificate. This hash is 20 bytes long and is specified as a hexadecimal string. Can be found under "Thumbprint" of
        ///     certificate's details, without whitespaces.
        /// </param>
        /// <exception cref="CommandPromptRequestException">Thrown if command exits with non-success exit code.</exception>
        public static void AddSslCert(string ipAddress, string port, string appId, string certHash)
        {
            try
            {
                CommandPrompt.Execute(
                    $"netsh http add sslcert ipport={ipAddress}:{port} appid={appId} certhash={certHash}");
            }
            catch (CommandPromptRequestException requestException)
            {
                // Try and repair the cert if getting Error 1312 and try again
                if (ParseNetshErrorCodeOutput(requestException.Output) != NetworkShellErrorCodes.LogOnSessionDoesNotExist)
                {
                    throw;
                }

                Trace.TraceWarning("netsh.exe Error 1312 usually means the certificate is missing \"unique container\".");
                Trace.TraceWarning("Attempting to repair and trying again. If repair does not work, reinstall the certificate. Certificate Hash: {0}", certHash);

                CertificateUtility.RepairCertificate(StoreName.My, certHash);
                CommandPrompt.Execute(
                    $"netsh http add sslcert ipport={ipAddress}:{port} appid={appId} certhash={certHash}");
            }
        }

        /// <summary>
        ///     Deletes SSL server certificate bindings and the corresponding client certificate policies for an IP address and port.
        ///     Command executes idempotently, ignoring "system cannot find the file specified" error.
        /// </summary>
        /// <param name="ipAddress">Specifies the IPv4 or IPv6 address portion of IpPort for which the SSL certificate bindings will be deleted.</param>
        /// <param name="port">Specifies the port portion of IpPort for which the SSL certificate bindings will be deleted.</param>
        /// <exception cref="CommandPromptRequestException">Thrown if command exits with non-success exit code.</exception>
        public static void DeleteSslCert(string ipAddress, string port)
        {
            try
            {
                CommandPrompt.Execute($"netsh http delete sslcert ipport={ipAddress}:{port}");
            }
            catch (CommandPromptRequestException requestException)
            {
                // Swallow Error:2 "system cannot find the file specified" since it is idempotent case for delete
                if (ParseNetshErrorCodeOutput(requestException.Output) != NetworkShellErrorCodes.CannotFindTheFileSpecified)
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///     Retrieves the error code from netsh.exe output. netsh.exe returns error codes in the output text that differ from the command's exit code.
        ///     Use this to retrieve the error code from output text.
        /// </summary>
        /// <param name="output">The output text from running netsh command. Must contain an error.</param>
        /// <returns>The error code parsed from the provided output.</returns>
        /// <exception cref="System.ArgumentException">Thrown if provided output does not contain an error code in format "Error: {code}"</exception>
        private static int ParseNetshErrorCodeOutput(string output)
        {
            Match match = Regex.Match(output, @"Error: (\S*)");
            if (!match.Success)
            {
                throw new ArgumentException($"Provided output did not contain an error code. Output: {output}", nameof(output));
            }

            // First entry in group contains "Error: " text, second entry is the actual error code
            int errorCode = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            return errorCode;
        }
    }

    /// <summary>
    ///     Error codes for netsh command.
    /// </summary>
    public static class NetworkShellErrorCodes
    {
        /// <summary>
        ///     Error:2 "system cannot find the file specified"
        /// </summary>
        public const int CannotFindTheFileSpecified = 2;

        /// <summary>
        ///     Error:1312 "A specified logon session does not exist. It may already have been terminated"
        /// </summary>
        public const int LogOnSessionDoesNotExist = 1312;
    }
}
