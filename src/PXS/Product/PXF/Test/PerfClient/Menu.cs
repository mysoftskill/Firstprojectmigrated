// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.PerfClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;

    /// <summary>
    /// Menu
    /// </summary>
    public class Menu
    {
        public string Header { get; set; }

        private Dictionary<int, Tuple<string, Action>> Items { get; set; }

        private int AutoChoiceCounter { get; set; }

        public Menu(string header)
        {
            this.Header = header;
            this.Items = new Dictionary<int, Tuple<string, Action>>();
            this.AutoChoiceCounter = 1;
        }

        public void Render()
        {
            Console.WriteLine(this.Header);
            foreach (var item in this.Items)
            {
                Console.WriteLine("{0}. {1}", item.Key, item.Value.Item1);
            }

            int choice = IOHelpers.GetUserInputInt(string.Empty);
            this.Items[choice].Item2();
        }

        public void AddItem(string displayName, Action callback)
        {
            this.AddItem(this.AutoChoiceCounter, displayName, callback);
            this.AutoChoiceCounter++;
        }

        public void AddItem(int choice, string displayName, Action callback)
        {
            this.Items.Add(choice, new Tuple<string, Action>(displayName, callback));
        }
    }
}