// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Email;

    /// <summary>
    ///     action that can send an email
    /// </summary>
    public class EmailSendAction : ActionOp<EmailSendDef>
    {
        public const string ActionType = "TRANSMIT-EMAIL";

        private readonly ITemplateStore templateStore;
        private readonly IMailSender sender;

        private EmailSendDef def;

        /// <summary>
        ///     Initializes a new instance of the LockAction class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        /// <param name="templateStore">template store</param>
        /// <param name="emailSender">email sender</param>
        public EmailSendAction(
            IModelManipulator modelManipulator,
            ITemplateStore templateStore,
            IMailSender emailSender) :
            base(modelManipulator)
        {
            this.templateStore = templateStore ?? throw new ArgumentNullException(nameof(templateStore));
            this.sender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        }

        /// <summary>
        ///     Gets the action type
        /// </summary>
        public override string Type => EmailSendAction.ActionType;

        /// <summary>
        ///     Gets the action's required parameters
        /// </summary>
        protected override ICollection<string> RequiredParams => EmailSendAction.Args.Required;

        /// <summary>
        ///     Allows a derived type to perform validation on the definition object created during parsing
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="factory">action factory</param>
        /// <param name="definition">definition object</param>
        /// <returns>true if the parse was successful, false if at least one error was found</returns>
        protected override bool ProcessAndStoreDefinition(
            IParseContext context,
            IActionFactory factory,
            EmailSendDef definition)
        {
            bool result = base.ProcessAndStoreDefinition(context, factory, definition);

            result = this.templateStore.ValidateReference(context, definition.Body) && result;
            result = this.templateStore.ValidateReference(context, definition.Subject) && result;

            this.def = definition;
            return result;
        }

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

            TemplateRef bodyTemplate = this.def.Body;
            EmailMessage message;
            object result;
            bool sent;

            if (string.IsNullOrWhiteSpace(argsActual.BodyTagOverride) == false)
            {
                bodyTemplate = new TemplateRef
                {
                    TemplateTag = argsActual.BodyTagOverride,
                    Parameters = bodyTemplate.Parameters,
                };
            }

            message = new EmailMessage
            {
                ToAddresses = argsActual.To,
                CcAddresses = argsActual.Cc,

                FromDisplayText = string.IsNullOrWhiteSpace(this.def.FromDisplayName) ? 
                    this.def.FromAddress :
                    this.def.FromDisplayName,

                ReplyTo = string.IsNullOrWhiteSpace(this.def.ReplyToAddress) ?
                    this.def.FromAddress :
                    this.def.ReplyToAddress,

                FromAddress = this.def.FromAddress,

                Subject = this.templateStore.Render(context, this.def.Subject, model)?.Trim(),
                Body = this.templateStore.Render(context, bodyTemplate, model),

                Priority = this.def.Priority,
            };

            if (message.ValidateAndNormalize(context) == false)
            {
                throw new ActionExecuteException(
                    $"Errors found validating email for {this.ObjText} [tag: {context.Tag}]");
            }

            try
            {
                // If we are not in simulation mode, send the results
                // If we are overriding simulation mode, send the result unless the email To line indicates that there is "NothingToSend" 
                if (context.IsSimulation == false || (this.def.IgnoreSimulationMode && !message.ToAddresses.Contains("NothingToSend")))
                {
                    sent = await this.sender.SendEmailAsync(context.CancellationToken, message, null);
                }
                else
                {
                    context.Log("Running in simulation mode.  Email will NOT be sent.");
                    sent = true;
                }

                context.IncrementCounter("Emails Sent", this.Tag, argsActual.CounterSuffix, 1);

                context.ReportActionEvent(
                    "success",
                    this.Type,
                    this.Tag,
                    new Dictionary<string, string> { { "Subject", message.Subject } });
            }
            catch (Exception e)
            {
                context.IncrementCounter("Email Sending Errors", this.Tag, argsActual.CounterSuffix, 1);

                context.ReportActionError(
                    "error",
                    this.Type,
                    this.Tag,
                    e.GetMessageAndInnerMessages(),
                    new Dictionary<string, string> { { "Subject", message.Subject } });

                throw;
            }

            context.Log(
                "{0} email message [{1}] to the following addresses [to: {2}] [cc: {3}]".FormatInvariant(
                    sent ? "Successfully sent" : "Failed to send",
                    message.Subject,
                    string.Join("; ", message.ToAddresses),
                    message.CcAddresses != null ? string.Join("; ", message.CcAddresses) : "<none>"));

            result = this.ModelManipulator
                .TransformFrom(
                    new
                    {
                        SendTime = sent ? context.NowUtc : (DateTimeOffset?)null,
                        Success = sent,
                        Subject = message.Subject,
                        From = message.FromAddress,
                        To = message.ToAddresses,
                        Cc = message.CcAddresses ?? ListHelper.EmptyList<string>(),
                    });

            return (true, result);
        }

        /// <summary>
        ///     Action arguments
        /// </summary>
        internal class Args : IValidatable
        {
            public static readonly string[] Required = { "To" };

            public ICollection<string> Cc { get; set; }
            public ICollection<string> To { get; set; }
            public string BodyTagOverride { get; set; }
            public string CounterSuffix { get; set; }

            /// <summary>
            ///     Validates the argument object and logs any errors to the context
            /// </summary>
            /// <param name="context">execution context</param>
            /// <returns>true if the object validated successfully; false otherwise</returns>
            public bool ValidateAndNormalize(IContext context)
            {
                if (this.CounterSuffix != null && string.IsNullOrWhiteSpace(this.CounterSuffix))
                {
                    context.LogError("null or non-empty counter suffix must be specified");
                    return false;
                }

                this.To = this.To
                    ?.Where(o => string.IsNullOrWhiteSpace(o) == false)
                    .Select(o => o.Trim())
                    .ToList();

                this.Cc = this.Cc
                    ?.Where(o => string.IsNullOrWhiteSpace(o) == false)
                    .Select(o => o.Trim())
                    .ToList();

                if (this.To == null || this.To.Count == 0)
                {
                    context.LogError("At least one 'to' address must be specified");
                    return false;
                }

                if (this.Cc == null || this.Cc.Count == 0)
                {
                    this.Cc = null;
                }

                return true;
            }
        }
    }
}
