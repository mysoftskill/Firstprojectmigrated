    // <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Mail;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Email;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class EmailSendActionTests
    {
        private class EmailSendActionTestException : Exception
        {
            public EmailSendActionTestException(string message) : base(message) {  }
        }

        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IExecuteContext> mockExecCtx = new Mock<IExecuteContext>();
        private readonly Mock<IActionFactory> mockFact = new Mock<IActionFactory>();
        private readonly Mock<ITemplateStore> mockTemplateStore = new Mock<ITemplateStore>();
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IActionStore> mockActionStore = new Mock<IActionStore>();
        private readonly Mock<IMailSender> mockSender = new Mock<IMailSender>();

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        private const string DefTag = "tag";

        private EmailSendAction testObj;

        private IExecuteContext execCtx;
        private IActionFactory fact;
        private IParseContext parseCtx;
        private IActionStore store;

        private (ActionRefCore, object, EmailSendDef, EmailSendAction.Args) SetupTestObj(
            bool? result = null,
            string to = null,
            string cc = null,
            string from = null,
            EmailSendDef def = null)
        {
            const string CounterSuffix = "CounterSuffix";

            object modelIn = new object();

            IDictionary<string, ModelValue> argXform = new Dictionary<string, ModelValue>()
            {
                { "CounterSuffix", new ModelValue { Const = CounterSuffix } },
                { "To", new ModelValue { Const = 1 } },
            };

            def = def ?? new EmailSendDef
            {
                Subject = new TemplateRef { Inline = "Text" },
                Body = new TemplateRef { Inline = "Text" },
                
                FromAddress = from,
            };

            EmailSendAction.Args args = new EmailSendAction.Args
            {
                CounterSuffix = CounterSuffix,
                To = to != null ? new[] { to } : null,
                Cc = cc != null ? new[] { cc } : null,
            };

            this.mockModel
                .Setup(o => o.TransformTo<EmailSendAction.Args>(It.IsAny<object>()))
                .Returns(args);

            this.mockSender
                .Setup(o => o.SendEmailAsync(It.IsAny<CancellationToken>(), It.IsAny<EmailMessage>(), It.IsAny<string>()))
                .ReturnsAsync(result ?? true);

            this.testObj = new EmailSendAction(
                this.mockModel.Object, this.mockTemplateStore.Object, this.mockSender.Object);

            Assert.IsTrue(
                this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, EmailSendActionTests.DefTag, def));

            Assert.IsTrue(this.testObj.ExpandDefinition(this.parseCtx, this.store));
            Assert.IsTrue(this.testObj.Validate(this.parseCtx, argXform));

            this.mockModel.Setup(o => o.MergeModels(this.execCtx, modelIn, null, argXform)).Returns(args);

            return (new ActionRefCore { ArgTransform = argXform }, modelIn, def, args);
        }

        [TestInitialize]
        public void Init()
        {
            this.mockExecCtx.SetupGet(o => o.NowUtc).Returns(DateTimeOffset.Parse("2006-04-15T15:01:00-07:00"));
            this.mockExecCtx.SetupGet(o => o.OperationStartTime).Returns(DateTimeOffset.Parse("2006-04-15T15:00:00-07:00"));
            this.mockExecCtx.SetupGet(o => o.CancellationToken).Returns(this.cancelSource.Token);
            this.mockExecCtx.SetupGet(o => o.IsSimulation).Returns(false);

            this.mockTemplateStore
                .Setup(o => o.Render(It.IsAny<IContext>(), It.IsAny<TemplateRef>(), It.IsAny<object>()))
                .Returns((IContext ctx, TemplateRef tref, object data) => tref.Inline);

            this.mockTemplateStore
                .Setup(o => o.ValidateReference(It.IsAny<IContext>(), It.IsAny<TemplateRef>())).Returns(true);

            this.parseCtx = this.mockParseCtx.Object;
            this.execCtx = this.mockExecCtx.Object;
            this.store = this.mockActionStore.Object;
            this.fact = this.mockFact.Object;
        }

        [TestMethod]
        public async Task ExecuteParsesArguments()
        {
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, _, _) = this.SetupTestObj(to: "to", from: "from");

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockModel.Verify(o => o.MergeModels(this.execCtx, modelIn, null, refCore.ArgTransform), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ActionExecuteException))]
        public async Task ExecuteThrowsIfEmailIsConstructedWithEmptySubject()
        {
            EmailSendDef def;
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, def, _) = this.SetupTestObj(to: "to", from: "from");

            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Subject, It.IsAny<object>())).Returns((string)null);

            try
            {
                await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);
            }
            catch (ActionExecuteException e)
            {
                Assert.IsTrue(e.Message.Contains("Errors found validating email"));
                throw;
            }
        }

        [TestMethod]
        public async Task ExecuteUsesBodyTagOverrideIfOneProvided()
        {
            EmailSendAction.Args args;
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, _, args) = this.SetupTestObj(to: "to", from: "from");

            this.mockTemplateStore
                .Setup(o => o.Render(It.IsAny<IContext>(), It.IsAny<TemplateRef>(), It.IsAny<object>()))
                .Returns((IContext ctx, TemplateRef tref, object data) => "OVERRIDETEMPLATE");

            args.BodyTagOverride = "BODYTAGOVERRIDE";

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockTemplateStore.Verify(
                o => o.Render(
                    this.execCtx,
                    It.Is<TemplateRef>(p => p.Inline == null && args.BodyTagOverride.Equals(p.TemplateTag)),
                    modelIn),
                Times.Once);
        }

        [TestMethod]
        public async Task ExecuteRendersBodyAndSubject()
        {
            EmailSendDef def;
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, def, _) = this.SetupTestObj(to: "to", from: "from");

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockTemplateStore.Verify(o => o.Render(this.execCtx, def.Body, modelIn), Times.Once);
            this.mockTemplateStore.Verify(o => o.Render(this.execCtx, def.Subject, modelIn), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteSendsEmail()
        {
            const string Subject = "subject";
            const string Body = "body";
            const string To = "to";
            const string Cc = "cc";

            ActionRefCore refCore;
            EmailSendDef def;
            object modelIn;

            def = new EmailSendDef
            {
                Subject = new TemplateRef { Inline = "Text" },
                Body = new TemplateRef { Inline = "Text" },

                Priority = MailPriority.High,

                ReplyToAddress = "replyTo",

                FromDisplayName = "fromdisplay",
                FromAddress = "from",
            };

            (refCore, modelIn, _, _) = this.SetupTestObj(to: To, cc: Cc, def: def);

            Func<EmailMessage, bool> validator =
                msg =>
                {
                    Assert.AreEqual(Subject, msg.Subject);
                    Assert.AreEqual(Body, msg.Body);

                    Assert.AreEqual(def.ReplyToAddress, msg.ReplyTo);

                    Assert.AreEqual(def.Priority, msg.Priority);

                    Assert.AreEqual(def.FromDisplayName, msg.FromDisplayText);
                    Assert.AreEqual(def.FromAddress, msg.FromAddress);

                    return true;
                };

            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Subject, It.IsAny<object>())).Returns(Subject);
            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Body, It.IsAny<object>())).Returns(Body);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // validate
            this.mockSender
                .Verify(o => o.SendEmailAsync(this.execCtx.CancellationToken, It.Is<EmailMessage>(p => validator(p)), null));
        }

        [TestMethod]
        public async Task ExecuteLogsEventAndIncrementsCounterOnSuccess()
        {
            const string Subject = "subject";
            const string Body = "body";
            const string To = "to";
            const string Cc = "cc";

            EmailSendAction.Args args;
            ActionRefCore refCore;
            EmailSendDef def;
            object modelIn;

            def = new EmailSendDef
            {
                Subject = new TemplateRef { Inline = "Text" },
                Body = new TemplateRef { Inline = "Text" },

                Priority = MailPriority.High,

                ReplyToAddress = "replyTo",

                FromDisplayName = "fromdisplay",
                FromAddress = "from",
            };

            (refCore, modelIn, _, args) = this.SetupTestObj(to: To, cc: Cc, def: def);

            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Subject, It.IsAny<object>())).Returns(Subject);
            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Body, It.IsAny<object>())).Returns(Body);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // validate
            this.mockExecCtx.Verify(
                o => o.ReportActionEvent(
                    "success", 
                    this.testObj.Type,
                    this.testObj.Tag,
                    It.Is<IDictionary<string, string>>(p => p["Subject"].Equals(Subject))));

            this.mockExecCtx.Verify(
                o => o.IncrementCounter("Emails Sent", this.testObj.Tag, args.CounterSuffix, 1));
        }

        [TestMethod]
        public async Task ExecuteLogsEventAndIncrementsCounterOnFailure()
        {
            const string ErrMsg = "ERROR";
            const string Subject = "subject";
            const string Body = "body";
            const string To = "to";
            const string Cc = "cc";

            EmailSendAction.Args args;
            ActionRefCore refCore;
            EmailSendDef def;
            object modelIn;

            def = new EmailSendDef
            {
                Subject = new TemplateRef { Inline = "Text" },
                Body = new TemplateRef { Inline = "Text" },

                Priority = MailPriority.High,

                ReplyToAddress = "replyTo",

                FromDisplayName = "fromdisplay",
                FromAddress = "from",
            };

            (refCore, modelIn, _, args) = this.SetupTestObj(to: To, cc: Cc, def: def);

            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Subject, It.IsAny<object>())).Returns(Subject);
            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Body, It.IsAny<object>())).Returns(Body);

            this.mockSender
                .Setup(o => o.SendEmailAsync(It.IsAny<CancellationToken>(), It.IsAny<EmailMessage>(), It.IsAny<string>()))
                .Returns(Task.FromException<bool>(new EmailSendActionTestException(ErrMsg)));

            try
            {
                // test
                await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);
                Assert.Fail("Did not throw");
            }
            catch (EmailSendActionTestException)
            {
                // validate
                this.mockExecCtx.Verify(
                    o => o.ReportActionError(
                        "error",
                        this.testObj.Type,
                        this.testObj.Tag,
                        ErrMsg,
                        It.Is<IDictionary<string, string>>(p => p["Subject"].Equals(Subject))));

                this.mockExecCtx.Verify(
                    o => o.IncrementCounter("Email Sending Errors", this.testObj.Tag, args.CounterSuffix, 1));
            }

        }

        [TestMethod]
        public async Task ExecuteReturnsEmailSendResult()
        {
            const string Subject = "subject";
            const string From = "from";
            const string To = "to";
            const string Cc = "cc";
            const bool Result = true;

            EmailSendDef def;
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, def, _) = this.SetupTestObj(to: To, cc: Cc, from: From);

            Func<object, bool> validator =
                source =>
                {
                    // this cast only works because this unit test assembly has "internals visible to" granted to it by the 
                    //  main assembly- source is an anonymous type and their generates class is declared internal
                    dynamic validateObj = source;

                    Assert.IsNotNull(source);

                    Assert.AreEqual(this.execCtx.NowUtc, validateObj.SendTime);
                    Assert.AreEqual(Result, validateObj.Success);

                    Assert.AreEqual(Subject, validateObj.Subject);
                    Assert.AreEqual(From, validateObj.From);
                    Assert.AreEqual(To, ((IEnumerable<string>)validateObj.To).First());
                    Assert.AreEqual(Cc, ((IEnumerable<string>)validateObj.Cc).First());

                    return true;
                };

            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Subject, It.IsAny<object>())).Returns(Subject);

            this.mockSender
                .Setup(o => o.SendEmailAsync(It.IsAny<CancellationToken>(), It.IsAny<EmailMessage>(), It.IsAny<string>()))
                .ReturnsAsync(Result);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // validate
            this.mockModel.Verify(o => o.TransformFrom(It.Is<object>(p => validator(p))));
        }
    }
}