// ---------------------------------------------------------------------------
// <copyright file="AggregatingModelManipulatorTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Utility.Model
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.TestUtility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class AggregatingModelManipulatorTests
    {
        private readonly Mock<IModelReader> mockDefReader = new Mock<IModelReader>();
        private readonly Mock<IModelReader> mockReader = new Mock<IModelReader>();
        private readonly Mock<IModelWriter> mockWriter = new Mock<IModelWriter>();
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private IContext ctx;

        private AggregatingModelManipulator testObj;

        [TestInitialize]
        public void Init()
        {
            this.ctx = this.mockCtx.Object;
        }

        private void SetupTestObj(char? prefix)
        {
            this.testObj = new AggregatingModelManipulator(
                prefix.HasValue ? new Dictionary<char, IModelReader> { { prefix.Value, this.mockReader.Object } } : null,
                this.mockDefReader.Object,
                this.mockWriter.Object);
        }

        [TestMethod]
        public void CreateEmptyCallsCreateEmptyOnWriter()
        {
            object expected = new object();

            object result;

            this.SetupTestObj(null);

            this.mockWriter.Setup(o => o.CreateEmpty()).Returns(expected);

            // test
            result = this.testObj.CreateEmpty();

            // verify
            Assert.AreSame(expected, result);
            this.mockWriter.Verify(o => o.CreateEmpty(), Times.Once);
        }

        [TestMethod]
        public void TransformFromCallsTransformFromOnWriter()
        {
            object expected = new object();
            object source = new object();

            object result;

            this.SetupTestObj(null);

            this.mockWriter.Setup(o => o.TransformFrom(It.IsAny<object>())).Returns(expected);

            // test
            result = this.testObj.TransformFrom(source);

            // verify
            Assert.AreSame(expected, result);
            this.mockWriter.Verify(o => o.TransformFrom(source), Times.Once);
        }

        [TestMethod]
        public void TransformToCallsTransformToOnWriter()
        {
            object expected = new object();
            object source = new object();

            object result;

            this.SetupTestObj(null);

            this.mockWriter.Setup(o => o.TransformTo<object>(It.IsAny<object>())).Returns(expected);

            // test
            result = this.testObj.TransformTo<object>(source);

            // verify
            Assert.AreSame(expected, result);
            this.mockWriter.Verify(o => o.TransformTo<object>(source), Times.Once);
        }

        [TestMethod]
        public void ToEnumerableCallsToEnumerableOnWriter()
        {
            IEnumerable expected = Enumerable.Empty<object>();
            object source = new object();

            object result;

            this.SetupTestObj(null);

            this.mockWriter.Setup(o => o.ToEnumerable(It.IsAny<object>())).Returns(expected);

            // test
            result = this.testObj.ToEnumerable(source);

            // verify
            Assert.AreSame(expected, result);
            this.mockWriter.Verify(o => o.ToEnumerable(source), Times.Once);
        }

        [TestMethod]
        public void AddSubmodelCallsAddSubmodelOnWriter()
        {
            const MergeMode Mode = MergeMode.ReplaceExisting;
            const string Path = "path";

            object expected = new object();
            object target = new object();
            object submodel = new object();

            object result;

            this.SetupTestObj(null);

            this.mockWriter
                .Setup(
                    o => o.AddSubmodel(
                        It.IsAny<IContext>(),
                        It.IsAny<object>(),
                        It.IsAny<string>(),
                        It.IsAny<object>(),
                        It.IsAny<MergeMode>()))
                .Returns(expected);

            // test
            result = this.testObj.AddSubmodel(this.ctx, target, Path, submodel, Mode);

            // verify
            Assert.AreSame(expected, result);
            this.mockWriter.Verify(o => o.AddSubmodel(this.ctx, target, Path, submodel, Mode), Times.Once);
        }

        [TestMethod]
        public void RemoveSubmodelCallsRemoveSubmodelOnWriter()
        {
            const string Path = "path";

            object expected = new object();
            object target = new object();

            object result;

            this.SetupTestObj(null);

            this.mockWriter.Setup(o => o.RemoveSubmodel(It.IsAny<object>(), It.IsAny<string>())).Returns(expected);

            // test
            result = this.testObj.RemoveSubmodel(target, Path);

            // verify
            Assert.AreSame(expected, result);
            this.mockWriter.Verify(o => o.RemoveSubmodel(target, Path), Times.Once);
        }

        [TestMethod]
        public void MergeModelsCallsMergeModelsOnWriter()
        {
            ICollection<KeyValuePair<string, ModelValue>> xform = new List<KeyValuePair<string, ModelValue>>();

            object expected = new object();
            object target = new object();
            object source = new object();

            object result;

            this.SetupTestObj(null);

            this.mockWriter
                .Setup(
                    o => o.MergeModels(
                        It.IsAny<IContext>(),
                        It.IsAny<IModelReader>(),
                        It.IsAny<object>(),
                        It.IsAny<object>(),
                        It.IsAny<ICollection<KeyValuePair<string, ModelValue>>>()))
                .Returns(expected);

            // test
            result = this.testObj.MergeModels(this.ctx, source, target, xform);

            // verify
            Assert.AreSame(expected, result);
            this.mockWriter.Verify(o => o.MergeModels(this.ctx, this.testObj, source, target, xform), Times.Once);
        }

        [TestMethod]
        public void MergeModelsWithReaderCallsMergeModelsWithSpecifiedReaderOnWriter()
        {
            ICollection<KeyValuePair<string, ModelValue>> xform = new List<KeyValuePair<string, ModelValue>>();

            object expected = new object();
            object target = new object();
            object source = new object();

            object result;

            this.SetupTestObj(null);

            this.mockWriter
                .Setup(
                    o => o.MergeModels(
                        It.IsAny<IContext>(),
                        It.IsAny<IModelReader>(),
                        It.IsAny<object>(),
                        It.IsAny<object>(),
                        It.IsAny<ICollection<KeyValuePair<string, ModelValue>>>()))
                .Returns(expected);

            // test
            result = this.testObj.MergeModels(this.ctx, null, source, target, xform);

            // verify
            Assert.AreSame(expected, result);
            this.mockWriter.Verify(o => o.MergeModels(this.ctx, null, source, target, xform), Times.Once);
        }

        [TestMethod]
        [DataRow("#.Path", null, true)]
        [DataRow("#.Path", '&', true)]
        [DataRow("#Path", '#', true)]
        [DataRow("#.Path", '#', false)]
        public void TryExtractCallsAppropriateReaderForPath(
            string path,
            char? prefix,
            bool expectDefault)
        {
            Mock<IModelReader> notExpectedReader;
            Mock<IModelReader> expectedReader;

            object expected = new object();
            object source = new object();

            object tryResult1;
            object tryResult2;

            object result;

            this.SetupTestObj(prefix);

            this.mockReader
                .Setup(
                    o => o.TryExtractValue(
                        It.IsAny<IContext>(),
                        It.IsAny<object>(),
                        It.IsAny<string>(),
                        It.IsAny<object>(),
                        out tryResult1))
                .OutCallback((IContext c, object p, string k, object d, out object r) => r = expectDefault ? null : expected)
                .Returns(true);

            this.mockDefReader
                .Setup(
                    o => o.TryExtractValue(
                        It.IsAny<IContext>(),
                        It.IsAny<object>(),
                        It.IsAny<string>(),
                        It.IsAny<object>(),
                        out tryResult2))
                .OutCallback((IContext c, object p, string k, object d, out object r) => r = expectDefault ? expected : null)
                .Returns(true);

            notExpectedReader = expectDefault ? this.mockReader : this.mockDefReader;
            expectedReader = expectDefault ? this.mockDefReader : this.mockReader;

            // test
            this.testObj.TryExtractValue(this.ctx, source, path, out result);

            // verify
            Assert.AreSame(expected, result);

            notExpectedReader
                .Verify(
                    o => o.TryExtractValue(
                        It.IsAny<IContext>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<object>(), out tryResult1),
                    Times.Never);

            expectedReader.Verify(o => o.TryExtractValue(this.ctx, source, path, null, out tryResult1), Times.Once);
        }
    }
}
