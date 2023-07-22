// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.DataSource
{
    using Microsoft.Membership.MemberServices.Common.Collections;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for random test data
    /// </summary>
    /// <typeparam name="T">Date type</typeparam>
    public abstract class StoreBase<T>
    {
        protected StoreBase(int minRandomItems, int maxRandomItems)
        {
            this.MinItems = minRandomItems;
            this.MaxItems = maxRandomItems;
        }

        /// <summary>
        /// Min items generated on requests from a new user
        /// </summary>
        protected int MinItems
        {
            get;
        }

        /// <summary>
        /// Max items generated on requests from a new user
        /// </summary>
        protected int MaxItems
        {
            get;
        }

        protected object UsersLock { get; } = new object();

        /// <summary>
        /// Store of previously generated user data
        /// </summary>
        protected LastRecentlyUsedDictionary<long, List<T>> Users { get; } = new LastRecentlyUsedDictionary<long, List<T>>(1000);

        /// <summary>
        /// Gets or generates data
        /// </summary>
        /// <param name="user">User Puid</param>
        /// <returns>Set of data for that user</returns>
        public List<T> Get(long user)
        {
            lock (this.UsersLock)
            {
                if (this.Users.TryGetValue(user, out var data) && data.Count > 0)
                {
                    return data;
                }
                else
                {
                    return this.Users[user] = this.CreateRandomTestData();
                }
            }
        }

        /// <summary>
        /// Deletes all items that match predicate for user, but leaves user in database.  
        /// </summary>
        /// <param name="user">User Puid</param>
        /// <param name="predicate">The predicate to match</param>
        /// <returns>True if any data was deleted</returns>
        public bool DeleteWhere(long user, Predicate<T> predicate)
        {
            lock (this.UsersLock)
            {
                if (this.Users.TryGetValue(user, out var results))
                {
                    return results.RemoveAll(predicate) > 0;
                }
            }

            return false;
        }

        /// <summary>
        /// Deletes all items for user, but leaves user in database.  
        /// </summary>
        /// <param name="user">User Puid</param>
        /// <returns>True if any data was deleted</returns>
        public bool DeleteAllItems(long user)
        {
            bool found = false;

            // find user
            lock (this.UsersLock)
            {
                if (found = this.Users.TryGetValue(user, out var results))
                {
                    results.RemoveRange(0, results.Count);
                }
            }

            return found;
        }

        /// <summary>
        /// delete all items for a user.  If you re-read this user, you will get new random data.  
        /// </summary>
        /// <param name="user">User Puid</param>
        /// <returns>True if the user was found and deleted</returns>
        public bool DeleteUser(long user)
        {
            lock (this.UsersLock)
            {
                return this.Users.Remove(user);
            }
        }

        /// <summary>
        /// Generates random data for the user
        /// </summary>
        /// <returns>Set of data</returns>
        protected abstract List<T> CreateRandomTestData();
    }
}
