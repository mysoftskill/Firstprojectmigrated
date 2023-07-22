// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Utility
{
    using System;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class UtilityTests
    {
        private readonly Mock<IModelManipulator> mockManip = new Mock<IModelManipulator>();
        private readonly Mock<IExecuteContext> mockCtx = new Mock<IExecuteContext>();

        private IModelManipulator manip;
        private IExecuteContext ctx;

        public class TestArgs : IValidatable
        {
            private readonly bool validateResult;

            public TestArgs(bool validateResult) { this.validateResult = validateResult; }
            public TestArgs() { this.validateResult = true; }

            public DateTimeOffset Start { get; set; }

            public string ConfigData { get; set; }

            /// <summary>
            ///     Validates the argument object and logs any errors to the context
            /// </summary>
            /// <param name="context">execution context</param>
            /// <returns>true if the object validated successfully; false otherwise</returns>
            public bool ValidateAndNormalize(IContext context) => this.validateResult;
        }
        
        [TestInitialize]
        public void Init()
        {
            this.manip = this.mockManip.Object;
            this.ctx = this.mockCtx.Object;

            this.mockManip
                .Setup(o => o.TransformTo<TestArgs>(It.IsAny<TestArgs>()))
                .Returns((TestArgs p) => p);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ActionExecuteException))]
        public void ExtractArgsLogsToContextAndThrowsIfInputModelNull()
        {
            try
            {
                Utility.ExtractObject<TestArgs>(this.ctx, this.manip, null);
            }
            catch (Exception)
            {
                this.mockCtx.Verify(
                    o => o.LogError(It.Is<string>(s => s.Contains("action requires a non-null parameter model"))),
                    Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ActionExecuteException))]
        public void ExtractArgsLogsToContextAndThrowsIfInputModelDoesNotValidate()
        {
            TestArgs input = new TestArgs(false);

            try
            {
                Utility.ExtractObject<TestArgs>(this.ctx, this.manip, input);
            }
            catch (Exception)
            {
                this.mockCtx.Verify(
                    o => o.LogError(It.Is<string>(s => s.Contains("did not validate after being sucessfully extracted"))),
                    Times.Once);
                throw;
            }
        }

        [TestMethod]
        public void ExtractArgsReturnsInputIfInputModelIsTheSelectedType()
        {
            object input = new TestArgs(true);

            object result;

            result = Utility.ExtractObject<TestArgs>(this.ctx, this.manip, input);

            Assert.AreSame(input, result);
        }
    }
}
