// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities
{
    using System;
    using System.Collections.Generic;

    public static class ViewManager
    {
        private static Stack<IView> Views;

        public static void Show()
        {
            Views.Peek().Render();
            Console.WriteLine();
        }

        public static void Initialize()
        {
            Views = new Stack<IView>();
        }

        public static void NavigateForwards(IView value)
        {
            Views.Push(value);
            Show();
        }

        public static void NavigateBackwards()
        {
            Views.Pop();
            Show();
        }
    }
}
