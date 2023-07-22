// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.TestClient
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

        private Dictionary<char, Tuple<string, Action>> Items { get; set; }

        private int AutoChoiceCounter { get; set; }

        public Menu(string header)
        {
            this.Header = header;
            this.Items = new Dictionary<char, Tuple<string, Action>>();
            this.AutoChoiceCounter = 1;
        }

        public void Render()
        {
            Console.WriteLine(this.Header);
            foreach (var item in this.Items)
            {
                Console.WriteLine("{0}. {1}", item.Key, item.Value.Item1);
            }

            char choice = IOHelpers.GetValidUserInputCharacter(this.Items.Keys.ToArray());
            this.Items[choice].Item2();
        }

        public void AddItem(string displayName, Action callback)
        {
            this.AddItem((char)(this.AutoChoiceCounter + '0'), displayName, callback);
            this.AutoChoiceCounter++;
        }

        public void AddItem(char choice, string displayName, Action callback)
        {
            this.Items.Add(choice, new Tuple<string, Action>(displayName, callback));
        }
    }
}