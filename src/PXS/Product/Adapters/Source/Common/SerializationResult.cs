// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    using System.Globalization;

    internal class SerializationResult<T>
    {
        public T Result { get; private set; }

        public string Error { get; private set; }

        public bool IsSuccess
        {
            get
            {
                return this.Error == null;
            }
        }

        public SerializationResult(T result)
        {
            this.Result = result;
        }

        public SerializationResult(string error)
        {
            this.Error = error;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "(IsSuccess={0}, Result={1}, Error={2})",
                                 this.IsSuccess, this.Result, this.Error);
        }
    }
}
