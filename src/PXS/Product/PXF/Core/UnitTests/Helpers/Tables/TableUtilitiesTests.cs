// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.Tables
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TableUtilitiesTests
    {
        [TestMethod]
        public void EscapeDoesNothingIfNoEscapableChars()
        {
            const string Test = "ThisIsAStringThatGetsNoEscaping";

            string result = TableUtilities.EscapeKey(Test);

            Assert.AreEqual(Test, result);
        }

        [TestMethod]
        public void EscapeCorrectlyEscapesAllEscapableChars()
        {
            const string Expected = "$24This$09Is$0aA$5cString$2fThat$23GetsALotOfEscaping$24";
            const string Test = "$This\tIs\nA\\String/That#GetsALotOfEscaping$";

            string result = TableUtilities.EscapeKey(Test);

            Assert.AreEqual(Expected, result);
        }

        [TestMethod]
        public void EscapeCorrectlyEscapesAllEscapableCharsAndIncludesTrailingNonEscapableChars()
        {
            const string Expected = "ThisIsAStringWith$24OneEscapedChar";
            const string Test = "ThisIsAStringWith$OneEscapedChar";

            string result = TableUtilities.EscapeKey(Test);

            Assert.AreEqual(Expected, result);
        }

        [TestMethod]
        public void UnescapeDoesNothingIfNoEscapeSequences()
        {
            const string Test = "ThisIsAStringThatGetsNoEscaping";

            string result = TableUtilities.UnescapeKey(Test);

            Assert.AreEqual(Test, result);
        }

        [TestMethod]
        public void UnescapeCorrectlyEscapesAllEscapeSequences()
        {
            const string Expected = "$This\tIs\nA\\String/That#GetsALotOfEscaping$";
            const string Test = "$24This$09Is$0aA$5cString$2fThat$23GetsALotOfEscaping$24";

            string result = TableUtilities.UnescapeKey(Test);

            Assert.AreEqual(Expected, result);
        }

        [TestMethod]
        public void UnescapeCorrectlyEscapesAllEscapeSequencesAndIncludesTrailingNonEscapableChars()
        {
            const string Expected = "ThisIsAStringWith$OneEscapedChar";
            const string Test = "ThisIsAStringWith$24OneEscapedChar";

            string result = TableUtilities.UnescapeKey(Test);

            Assert.AreEqual(Expected, result);
        }

    }
}
