// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility;

    /// <summary>
    ///     an action that acquires a lease to ensure only one instance of the contained actions are run within the lease period
    /// </summary>
    /// <remarks>
    ///     the lease has two modes:
    ///       initial lease time that the contained actions are expected to run in
    ///       lease extension time that is the time the lease is extended by upon successful completion of the contained actions
    /// </remarks>
    public class LockActionSet : ActionSet<LockActionSetDef>
    {
        public const string ActionType = "LOCK-TABLE";

        private static readonly string UniquifierPrefix = "." + Environment.MachineName + ".";

        private readonly ILockManager lockMgr;

        /// <summary>
        ///     Initializes a new instance of the LockAction class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        /// <param name="lockMgr">lock manager</param>
        public LockActionSet(
            IModelManipulator modelManipulator,
            ILockManager lockMgr) :
            base(modelManipulator)
        {
            this.lockMgr = lockMgr ?? throw new ArgumentNullException(nameof(lockMgr));
        }

        /// <summary>
        ///     Gets the action type
        /// </summary>
        public override string Type => LockActionSet.ActionType;

        /// <summary>
        ///     Gets the action's required parameters
        /// </summary>
        protected override ICollection<string> RequiredParams => LockActionSet.Args.Required;

        /// <summary>
        ///     Executes the action using the specified input
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">action reference</param>
        /// <param name="model">model from previous actions in the containing action set</param>
        /// <returns>execution result</returns>
        protected override async Task<(bool Continue, object Result)> ExecuteInternalAsync(
            IExecuteContext context,
            ActionRefCore actionRef,
            object model)
        {
            object args = this.ModelManipulator.MergeModels(context, model, null, actionRef.ArgTransform);
            Args argsActual = Utility.ExtractObject<Args>(context, this.ModelManipulator, args);
            bool actionSetResult = true;

            string owner = context.Tag + LockActionSet.UniquifierPrefix + Guid.NewGuid().ToString("N");

            ILockLease lease;
            string lockText = $"[{argsActual.LockGroupName}] lock for item [{argsActual.LockName}]";
            bool isComplete = false;

            // get and initialize the local model to use for the lock's action set
            model = this.GetActionSetLocalModel(context, null, model);

            lease = await this.lockMgr
                .AttemptAcquireAsync(
                    argsActual.LockGroupName, 
                    argsActual.LockName, 
                    owner, 
                    argsActual.LeaseTime, 
                    true)
                .ConfigureAwait(false);

            if (lease != null)
            {
                try
                {
                    context.LogVerbose("Acquried " + lockText);

                    context.CancellationToken.ThrowIfCancellationRequested();

                    actionSetResult = await this.ExecuteActionSetAsync(context, model).ConfigureAwait(false);
                    isComplete = true;
                }
                finally
                {
                    if (isComplete == false || argsActual.RunFrequency.HasValue == false)
                    {
                        context.LogVerbose("Releasing " + lockText);
                        await lease.ReleaseAsync(false).ConfigureAwait(false);
                    }
                    else
                    {
                        const string Msg =
                            "Extending lease on [{0}] by {1:d\\.hh\\:mm\\:ss} (date: {2:yyyy-MM-dd HH:mm:ssZ}) to ensure a run " +
                            "frequency of at least {3:d\\.hh\\:mm\\:ss}";

                        DateTimeOffset now = context.NowUtc;
                        TimeSpan frequency = argsActual.RunFrequency.Value;
                        TimeSpan extension = frequency - (now - context.OperationStartTime);

                        if (extension <= TimeSpan.Zero)
                        {
                            context.LogVerbose("Releasing " + lockText);
                            await lease.ReleaseAsync(false).ConfigureAwait(false);
                        }
                        else
                        {
                            DateTimeOffset nextRun = context.OperationStartTime + frequency;

                            context.Log(Msg.FormatInvariant(lockText, extension, nextRun, frequency));

                            // this ensures that the task cannot run again until lockTime expires
                            await lease.RenewAsync(extension).ConfigureAwait(false);
                        }
                    }
                }
            }
            else
            {
                context.Log(
                    $"Failed to aquire [" + lockText + "]- not executing contained action set");
            }

            return (actionSetResult && (argsActual.ReportContinueOnLockFailure || lease != null), model);
        }

        /// <summary>
        ///     Action arguments
        /// </summary>
        internal class Args : IValidatable
        {
            public static readonly string[] Required = { "LeaseTime", "LockGroupName", "LockName" };

            public TimeSpan? RunFrequency { get; set; }
            public TimeSpan LeaseTime { get; set; }
            public string LockGroupName { get; set; }
            public string LockName { get; set; }
            public bool ReportContinueOnLockFailure { get; set; }

            /// <summary>
            ///     Validates the argument object and logs any errors to the context
            /// </summary>
            /// <param name="context">execution context</param>
            /// <returns>true if the object validated successfully; false otherwise</returns>
            public bool ValidateAndNormalize(IContext context)
            {
                bool isValid = true;

                if (this.LeaseTime <= TimeSpan.Zero)
                {
                    context.LogError("Lease time must be greater than 0");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(this.LockGroupName))
                {
                    context.LogError("a non-empty lock group name must be specified");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(this.LockName))
                {
                    context.LogError("a non-empty lock name must be specified");
                    isValid = false;
                }

                return isValid;
            }
        }
    }
}
