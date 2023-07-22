// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.UnitTests.Store
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TemplateStoreTests
    {
        private readonly Mock<ITemplateAccessor> mockAccessor = new Mock<ITemplateAccessor>(MockBehavior.Strict);
        private readonly Mock<IParsedTemplate> mockTemplateUpdate = new Mock<IParsedTemplate>(MockBehavior.Strict);
        private readonly Mock<IParsedTemplate> mockTemplate = new Mock<IParsedTemplate>(MockBehavior.Strict);
        private readonly Mock<ITemplateParser> mockParser = new Mock<ITemplateParser>(MockBehavior.Strict);
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private const string RenderResult = "Result";
        private const string UpdateTag = "UpdateTAG";
        private const string Tag = "TAG";

        private static readonly TemplateDef Def = new TemplateDef
        {
            Text = "SOMETEXT",
            Tag = TemplateStoreTests.Tag,
        };

        private static readonly TemplateDef UpdateDef = new TemplateDef
        {
            Text = "SOMETEXTUPDATE",
            Tag = TemplateStoreTests.UpdateTag,
        };

        private TemplateStore testObj;

        private IContext ctx;

        [TestInitialize]
        public void Init()
        {
            this.mockParser
                .Setup(o => o.Parse(It.IsAny<IContext>(), It.IsAny<TemplateDef>()))
                .Returns(this.mockTemplate.Object);

            this.mockParser
                .Setup(o => o.Parse(It.IsAny<IContext>(), It.Is<TemplateDef>(p => TemplateStoreTests.UpdateTag.Equals(p.Tag))))
                .Returns(this.mockTemplateUpdate.Object);

            this.mockTemplate.SetupGet(o => o.Tag).Returns(TemplateStoreTests.Tag);
            this.mockTemplate
                .Setup(
                    o => o.Render(
                        It.IsAny<IContext>(), 
                        It.IsAny<ICollection<KeyValuePair<string, ModelValue>>>(), 
                        It.IsAny<object>()))
                .Returns(TemplateStoreTests.RenderResult);

            this.mockTemplateUpdate.SetupGet(o => o.Tag).Returns(TemplateStoreTests.UpdateTag);
            this.mockTemplateUpdate
                .Setup(
                    o => o.Render(
                        It.IsAny<IContext>(),
                        It.IsAny<ICollection<KeyValuePair<string, ModelValue>>>(),
                        It.IsAny<object>()))
                .Returns(TemplateStoreTests.RenderResult);

            this.mockAccessor
                .Setup(o => o.RetrieveTemplatesAsync())
                .ReturnsAsync(new List<TemplateDef> { TemplateStoreTests.Def });

            this.mockAccessor
                .Setup(
                    o => o.WriteTemplateChangesAsync(
                        It.IsAny<ICollection<string>>(),
                        It.IsAny<ICollection<TemplateDef>>(),
                        It.IsAny<ICollection<TemplateDef>>()))
                .Returns(Task.CompletedTask);

            this.ctx = this.mockCtx.Object;

            this.testObj = new TemplateStore(this.mockAccessor.Object, this.mockParser.Object);
        }

        [TestMethod]
        public async Task EnumerateReturnsEmptyListWhenRefreshDidNotPreserveDefs()
        {
            ICollection<TemplateDef> result;

            await this.testObj.RefreshAsync(this.ctx, false);

            // test
            result = this.testObj.EnumerateTemplates();

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task EnumerateReturnsPassedInActionsWhenRefreshDidPreserveDefs()
        {
            ICollection<TemplateDef> result;

            await this.testObj.RefreshAsync(this.ctx, true);

            // test
            result = this.testObj.EnumerateTemplates();

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreSame(TemplateStoreTests.Def, result.First());
        }

        [TestMethod]
        public void EnumerateReturnsEmptyListIfStoreNotInitialized()
        {
            ICollection<TemplateDef> result;

            // test
            result = this.testObj.EnumerateTemplates();

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ValidateThrowsWhenStoreNotInitialized()
        {
            this.testObj.ValidateReference(
                this.mockCtx.Object,
                new TemplateRef { TemplateTag = "notExist" });
        }

        [TestMethod]
        public async Task ValidateReturnsFalseWhenActionDoesNotExist()
        {
            bool result;

            await this.testObj.RefreshAsync(this.ctx, false);

            // test
            result = this.testObj.ValidateReference(
                this.mockCtx.Object,
                new TemplateRef { TemplateTag = "notExist" });

            // verify
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidateReturnsTrueWhenActionDoesExist()
        {
            bool result;

            await this.testObj.RefreshAsync(this.ctx, false);

            // test
            result = this.testObj.ValidateReference(
                this.mockCtx.Object,
                new TemplateRef { TemplateTag = TemplateStoreTests.Def.Tag });

            // verify
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void RenderReturnsEmptyWhenStoreNotInitialized()
        {
            IDictionary<string, object> data = new Dictionary<string, object>();
            TemplateRef tref = new TemplateRef
            {
                TemplateTag = TemplateStoreTests.Tag,
                Parameters = new Dictionary<string, ModelValue>(),
            };

            string result;

            // test
            result = this.testObj.Render(this.ctx, tref, data);

            // verify
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task RenderReturnsResultOfRenderOfParsedTemplateWhenTemplateIsFound()
        {
            IDictionary<string, object> data = new Dictionary<string, object>();

            TemplateRef tref = new TemplateRef
            {
                TemplateTag = TemplateStoreTests.Tag,
                Parameters = new Dictionary<string, ModelValue>(),
            };

            string result;

            await this.testObj.RefreshAsync(this.ctx, false);

            // test
            result = this.testObj.Render(this.ctx, tref, data);

            // verify
            this.mockTemplate.Verify(o => o.Render(this.ctx, tref.Parameters, data), Times.Once);
            Assert.AreEqual(TemplateStoreTests.RenderResult, result);
        }

        [TestMethod]
        public void RenderParsesAndReturnsResultOfRenderOfInlineTemplate()
        {
            Mock<ITemplateAccessor> localMockAccessor = new Mock<ITemplateAccessor>();

            IDictionary<string, object> data = new Dictionary<string, object>();

            TemplateRef tref = new TemplateRef
            {
                Inline = TemplateStoreTests.RenderResult,
                Parameters = new Dictionary<string, ModelValue>(),
            };

            TemplateStore localTestObj = new TemplateStore(localMockAccessor.Object, this.mockParser.Object);

            string result;

            Func<TemplateDef, bool> validator = p => { Assert.AreEqual(tref.Inline, p.Text); return true; };

            // test
            result = localTestObj.Render(this.ctx, tref, data);

            // verify
            this.mockParser.Verify(o => o.Parse(this.ctx, It.Is<TemplateDef>((p => validator(p)))), Times.Once);
            this.mockTemplate.Verify(o => o.Render(this.ctx, tref.Parameters, data), Times.Once);
            Assert.AreEqual(TemplateStoreTests.RenderResult, result);
        }

        [TestMethod]
        public async Task RefreshPullsTemplateDataFromTheRetrieverAndParsesIt()
        {
            // test
            await this.testObj.RefreshAsync(this.ctx, false);

            // verify
            this.mockAccessor.Verify(o => o.RetrieveTemplatesAsync(), Times.Once);
            this.mockParser.Verify(o => o.Parse(this.ctx, TemplateStoreTests.Def), Times.Once);
            Assert.AreEqual(1, this.testObj.Count);
            Assert.AreEqual(TemplateStoreTests.Def.Tag, this.testObj.GetTemplate(TemplateStoreTests.Def.Tag).Tag);
        }

        [TestMethod]
        public async Task RefreshAddsErrorToContextIfDuplicates()
        {
            this.mockAccessor
                .Setup(o => o.RetrieveTemplatesAsync())
                .ReturnsAsync(new List<TemplateDef> { TemplateStoreTests.Def, TemplateStoreTests.Def });

            // test
            await this.testObj.RefreshAsync(this.ctx, false);

            // verify
            this.mockCtx.Verify(
                o => o.LogError(
                    It.Is<string>(
                        p => p.Contains(
                            "Template store contains duplicate of the following tags: " + TemplateStoreTests.Tag))));
        }

        [TestMethod]
        public async Task UpdateParsesTheNewTemplates()
        {
            await this.testObj.RefreshAsync(this.ctx, false);

            // test
            await this.testObj.UpdateAsync(this.ctx, null, new[] { TemplateStoreTests.UpdateDef } );

            // verify
            this.mockParser.Verify(o => o.Parse(this.ctx, TemplateStoreTests.UpdateDef), Times.Once);
        }

        [TestMethod]
        public async Task UpdateAddsNewTemplateToStoreAndCommitsToStore()
        {
            await this.testObj.RefreshAsync(this.ctx, false);

            // test
            await this.testObj.UpdateAsync(this.ctx, null, new[] { TemplateStoreTests.UpdateDef });

            // verify
            Assert.AreEqual(2, this.testObj.Count);
            Assert.AreSame(this.mockTemplateUpdate.Object, this.testObj.GetTemplate(TemplateStoreTests.UpdateTag));

            this.mockAccessor
                .Verify(
                    o => o.WriteTemplateChangesAsync(
                        It.Is<ICollection<string>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<TemplateDef>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<TemplateDef>>(
                            p => p.Count == 1 && TemplateStoreTests.UpdateTag.EqualsIgnoreCase(p.First().Tag))),
                    Times.Once);
        }

        [TestMethod]
        public async Task UpdateReturnsTrueAndOverwritesExistingIfTagSameAsExistingTag()
        {
            bool result;

            await this.testObj.RefreshAsync(this.ctx, false);

            // test
            result = await this.testObj.UpdateAsync(
                this.mockCtx.Object,
                null,
                new[] { TemplateStoreTests.Def });

            // verify
            Assert.IsTrue(result);

            this.mockCtx.Verify(
                o => o.LogVerbose(It.Is<string>(p => p.Contains("Replaced existing template"))),
                Times.AtLeastOnce);

            Assert.AreSame(this.mockTemplate.Object, this.testObj.GetTemplate(TemplateStoreTests.Def.Tag));

            this.mockAccessor
                .Verify(
                    o => o.WriteTemplateChangesAsync(
                        It.Is<ICollection<string>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<TemplateDef>>(
                            p => p.Count == 1 && TemplateStoreTests.Tag.EqualsIgnoreCase(p.First().Tag)),
                        It.Is<ICollection<TemplateDef>>(p => p == null || p.Count == 0)),
                    Times.Once);
        }

        [TestMethod]
        public async Task UpdateRemovesActionFromStoreWhenActionExists()
        {
            bool result;

            await this.testObj.RefreshAsync(this.ctx, false);

            // test
            result = await this.testObj.UpdateAsync(
                this.mockCtx.Object,
                new[] { TemplateStoreTests.Tag },
                null);

            // verify
            Assert.IsTrue(result);

            Assert.IsNull(this.testObj.GetTemplate(TemplateStoreTests.Tag));

            this.mockAccessor
                .Verify(
                    o => o.WriteTemplateChangesAsync(
                        It.Is<ICollection<string>>(p => p.Count == 1 && TemplateStoreTests.Tag.EqualsIgnoreCase(p.First())),
                        It.Is<ICollection<TemplateDef>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<TemplateDef>>(p => p == null || p.Count == 0)),
                    Times.Once);
        }

        [TestMethod]
        public async Task UpdateIgnoresRemoveActionInstructionWhenWhenActionDoesNotExists()
        {
            bool result;

            await this.testObj.RefreshAsync(this.ctx, false);

            // test
            result = await this.testObj.UpdateAsync(
                this.mockCtx.Object,
                new[] { TemplateStoreTests.UpdateTag },
                null);

            // verify
            Assert.IsTrue(result);

            this.mockAccessor
                .Verify(
                    o => o.WriteTemplateChangesAsync(
                        It.Is<ICollection<string>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<TemplateDef>>(p => p == null || p.Count == 0),
                        It.Is<ICollection<TemplateDef>>(p => p == null || p.Count == 0)),
                    Times.Once);
        }
    }
}
