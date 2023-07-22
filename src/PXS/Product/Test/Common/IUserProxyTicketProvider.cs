// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.Test.Common
{
    using System.Threading.Tasks;

    public interface IUserProxyTicketProvider
    {
        /// <summary>
        /// Retrieves a compact RPS ticket (user ticket) for the given user.
        /// </summary>
        /// <param name="userName">User name (e.g. email address)</param>
        /// <param name="password">Password</param>
        /// <param name="maxRetryCount">max retry count</param>
        /// <returns>Result containing the compact ticket.</returns>
        Task<string> GetUserTicket(string userName, string password, int maxRetryCount);
        
        /// <summary>
        /// Retrieves a compact RPS ticket (user ticket) for the given user, then validates the ticket and returns the converted
        /// user proxy ticket.
        /// </summary>
        /// <param name="userName">User name (e.g. email address)</param>
        /// <param name="password">Password</param>
        /// <returns>Result containing either the user proxy ticket or an error message.</returns>
        Task<UserProxyTicketResult> GetTicket(string userName, string password);

        /// <summary>
        /// Retrieves a compact RPS ticket (user ticket) for the given user, then validates the ticket and returns the converted 
        /// user proxy ticket.
        /// </summary>
        /// <param name="userName">The username of the MSA account (email address)</param>
        /// <param name="password">The password of the MSA account</param>
        /// <returns>The retrieved user proxy ticket</returns>
        Task<string> GetTicketAsync(string userName, string password);
    }
}
