// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    ///     ExceptionExtensions class
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        ///      returns a string containing the exception message and all inner exceptions
        /// </summary>
        /// <param name="e">e</param>
        /// <returns>resulting value</returns>
        public static string GetMessageAndInnerMessages(this Exception e)
        {
            Stack<(Exception Exception, int Level)> stack = new Stack<(Exception Exception, int Level)>();
            StringBuilder sb = new StringBuilder();

            if (e.InnerException == null && e is AggregateException == false)
            {
                return e.Message;
            }

            stack.Push((e, 0));

            while(stack.Count > 0)
            {
                (Exception Exception, int Level) item = stack.Pop();
                int nextLevel = item.Level + 1;

                sb.Append(' ', item.Level * 2);
                sb.Append(item.Exception.GetType().FullName);
                sb.Append(": ");
                sb.Append(item.Exception.Message);
                sb.Append(' ');
                sb.Append('=', item.Level);
                sb.AppendLine(">");

                if (item.Exception is AggregateException aggregate && aggregate.InnerExceptions != null)
                {
                    foreach (Exception inner in aggregate.InnerExceptions)
                    {
                        stack.Push((inner, nextLevel));
                    }
                }
                else if (item.Exception.InnerException != null)
                {
                    stack.Push((item.Exception.InnerException, nextLevel));
                }
            }

            return sb.ToString();
        }
    }
}
