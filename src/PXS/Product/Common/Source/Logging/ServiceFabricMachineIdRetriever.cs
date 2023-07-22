// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging
{
    using System;
    using System.Diagnostics;
    using System.Fabric;

    public class ServiceFabricMachineIdRetriever : IMachineIdRetriever
    {
        private string machineId;

        /// <summary>
        ///     Reads the machine id
        /// </summary>
        /// <returns>The machine id</returns>
        public string ReadMachineId()
        {
            if (this.machineId == null)
            {
                try
                {
                    this.machineId = FabricRuntime.GetNodeContext()?.NodeName ?? "undefined";
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                    this.machineId = "undefined";
                }
            }

            return this.machineId;
        }
    }
}
