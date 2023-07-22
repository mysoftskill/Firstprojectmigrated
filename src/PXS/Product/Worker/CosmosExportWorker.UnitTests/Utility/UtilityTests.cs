// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text.RegularExpressions;

    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UtilityTests
    {
        //////////////////////////////////////////////////////////////////////////////////////////////
        // EnsureNoLeadingSlash 

        [TestMethod]
        [DataRow("/base/suffix/", "base/suffix/")]
        [DataRow("base/suffix/", "base/suffix/")]
        [DataRow("", "")]
        [DataRow(null, null)]
        public void EnsureNoLeadingSlashBehavesAsExpected(
            string input,
            string expected)
        {
            string result = Utility.EnsureNoLeadingSlash(input);
            Assert.AreEqual(expected, result);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        // EnsureTrailingSlash 

        [TestMethod]
        [DataRow("/base/suffix", "/base/suffix/")]
        [DataRow("/base/suffix/", "/base/suffix/")]
        [DataRow("", "")]
        [DataRow(null, null)]
        public void EnsureTrailingSlashBehavesAsExpected(
            string input,
            string expected)
        {
            string result = Utility.EnsureTrailingSlash(input);
            Assert.AreEqual(expected, result);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        // EnsureHasTrailingSlashButNoLeadingSlash 

        [TestMethod]
        [DataRow("base/suffix", "base/suffix/")]
        [DataRow("base/suffix/", "base/suffix/")]
        [DataRow("/base/suffix", "base/suffix/")]
        [DataRow("/base/suffix/", "base/suffix/")]
        [DataRow("", "")]
        [DataRow(null, null)]
        public void EnsureNoLeadingSlashAndTrailingSlashhBehavesAsExpected(
            string input,
            string expected)
        {
            string result = Utility.EnsureHasTrailingSlashButNoLeadingSlash(input);
            Assert.AreEqual(expected, result);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        // GenerateDataFileTag 

        [TestMethod]
        public void GenerateDataFileTagCorrectlyGeneratesTag()
        {
            const string CosmosTag = "AGENT";
            const string Agent = "AGENT";
            const string File = "FILE_2006_04_15";

            const string Expected = CosmosTag + "." + Agent + "." + File;

            string result;

            // test
            result = Utility.GenerateFileTag(CosmosTag, Agent, File);

            // verify
            Assert.AreEqual(Expected, result);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        // GenerateFileTagFromUri 

        [TestMethod]
        public void GenerateFileTagFromUriGeneratesTagForUriWithFilename()
        {
            const string CosmosTag = "AGENT";
            const string Agent = "AGENT";
            const string File = "DataFile_1_2_3_4.txt";
            const string Uri = "https://server.com/path/" + File;

            const string Expected = CosmosTag + "." + Agent + "." + File;

            string result;

            // test
            result = Utility.GenerateFileTagFromUri(CosmosTag, Agent, Uri);

            // verify
            Assert.AreEqual(Expected, result);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        // CanonicalizeCommandId 

        [TestMethod]
        public void CanonicalizeCommandIdCorrectlyCanonocalizesCommandId()
        {
            string guid = Guid.NewGuid().ToString("N");
            string expected = guid.Replace("-", string.Empty).ToLowerInvariant();

            string result;

            // test
            result = Utility.CanonicalizeCommandId(guid);

            // verify
            Assert.AreEqual(expected, result);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        // ExtractDataFileName 

        private static readonly Regex FileMatchTxt = new Regex(@"^(?<fileName>.*).txt$");
        private static readonly Regex FileMatch4 = new Regex(@"^(?<fileName>.*)_\d+_\d+_\d+_\d+\.txt$");
        private static readonly Regex FileMatch3 = new Regex(@"^(?<fileName>.*)_\d+_\d+_\d+\.txt$");

        private static readonly IReadOnlyList<Regex> DataFileNameRegexSet = new
            ReadOnlyCollection<Regex>(
                new[]
                {
                    UtilityTests.FileMatch4,
                    UtilityTests.FileMatch3,
                    UtilityTests.FileMatchTxt
                });

        [TestMethod]
        public void ExtractDataFileNameReturnsPrefixWhenFedNameWithFourPartSuffix()
        {
            const string Name = "NAME_A";

            ISet<string> currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int uniqueifier = 0;

            string result;

            // test
            (result, uniqueifier) = Utility.ExtractDataFileName(
                UtilityTests.DataFileNameRegexSet,
                currentFiles,
                Name + "_2006_04_15_15.txt",
                uniqueifier,
                0);

            // verify
            Assert.AreEqual(Name, result);
            Assert.AreEqual(1, currentFiles.Count);
            Assert.IsTrue(currentFiles.Contains(result));
            Assert.AreEqual(0, uniqueifier);
        }

        [TestMethod]
        public void ExtractDataFileNameReturnsPrefixWhenFedNameWithThreePartSuffix()
        {
            const string Name = "NAME_A";

            ISet<string> currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int uniqueifier = 0;

            string result;

            // test
            (result, uniqueifier) = Utility.ExtractDataFileName(
                UtilityTests.DataFileNameRegexSet,
                currentFiles,
                Name + "_2006_04_15.txt",
                uniqueifier,
                0);

            // verify
            Assert.AreEqual(Name, result);
            Assert.AreEqual(1, currentFiles.Count);
            Assert.IsTrue(currentFiles.Contains(result));
            Assert.AreEqual(0, uniqueifier);
        }

        [TestMethod]
        public void ExtractDataFileNameReturnsPrefixWhenFedNameWithJustTxtSuffix()
        {
            const string Name = "NAME_A";

            ISet<string> currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int uniqueifier = 0;

            string result;

            // test
            (result, uniqueifier) = Utility.ExtractDataFileName(
                UtilityTests.DataFileNameRegexSet,
                currentFiles,
                Name + ".txt",
                uniqueifier,
                0);

            // verify
            Assert.AreEqual(Name, result);
            Assert.IsTrue(currentFiles.Contains(result));
            Assert.AreEqual(1, currentFiles.Count);
            Assert.AreEqual(0, uniqueifier);
        }

        [TestMethod]
        public void ExtractDataFileNameAppendsUniqueifierIfNameAlreadyExists()
        {
            const string Name = "NAME_A";

            ISet<string> currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int uniqueifier = 0;

            string result;

            currentFiles.Add(Name);

            // test
            (result, uniqueifier) = Utility.ExtractDataFileName(
                UtilityTests.DataFileNameRegexSet,
                currentFiles,
                Name + "_2006_04_15.txt",
                uniqueifier,
                0);

            // verify
            Assert.AreEqual(Name + "0", result);
            Assert.AreEqual(2, currentFiles.Count);
            Assert.IsTrue(currentFiles.Contains(result));
            Assert.AreEqual(1, uniqueifier);
        }

        [TestMethod]
        public void ExtractDataFileStartsAtSpecifiedIndexEventIfAnEarlierMatchRegexWouldWork()
        {
            const string Name = "NAME_10";

            ISet<string> currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int uniqueifier = 0;

            string result;

            // test
            (result, uniqueifier) = Utility.ExtractDataFileName(
                UtilityTests.DataFileNameRegexSet,
                currentFiles,
                Name + "_2006_04_15.txt",
                uniqueifier,
                1);

            // verify
            Assert.AreEqual(Name, result);
            Assert.IsTrue(currentFiles.Contains(result));
            Assert.AreEqual(1, currentFiles.Count);
            Assert.AreEqual(0, uniqueifier);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        // ParseTemplateFileName 

        private static readonly Regex FileMatchManifestTxt = new Regex(@"^(?<fileName>.*).txt$");

        private static readonly Regex FileMatchManifest4 =
            new Regex(@"^(?<fileName>.*)_(?<year>\d+)_(?<month>\d+)_(?<day>\d+)_(?<hour>\d+)\.txt$");

        private static readonly Regex FileMatchManifest3 =
            new Regex(@"^(?<fileName>.*)_(?<year>\d+)_(?<month>\d+)_(?<day>\d+)\.txt$");

        private static readonly IReadOnlyList<Regex> ManifestFileNameRegexSet = new
            ReadOnlyCollection<Regex>(
                new[]
                {
                    UtilityTests.FileMatchManifest4,
                    UtilityTests.FileMatchManifest3,
                    UtilityTests.FileMatchManifestTxt
                });

        [TestMethod]
        public void ParseTemplateFileNameReturnsPrefixWhenFedNameWithFourPartSuffix()
        {
            const string Name = "NAME_A";

            TemplateParseResult result;

            // test
            result = Utility.ParseTemplateFileName(UtilityTests.ManifestFileNameRegexSet, Name + "_2006_04_15_14.txt");

            // verify
            Assert.AreEqual(0, result.PatternIndex);
            Assert.AreEqual("2006", result.Year);
            Assert.AreEqual("04", result.Month);
            Assert.AreEqual("15", result.Day);
            Assert.AreEqual("14", result.Hour);
        }

        [TestMethod]
        public void ParseTemplateFileNameReturnsPrefixWhenFedNameWithThreePartSuffix()
        {
            const string Name = "NAME_A";

            TemplateParseResult result;

            // test
            result = Utility.ParseTemplateFileName(UtilityTests.ManifestFileNameRegexSet, Name + "_2006_04_15.txt");

            // verify
            Assert.AreEqual(1, result.PatternIndex);
            Assert.AreEqual("2006", result.Year);
            Assert.AreEqual("04", result.Month);
            Assert.AreEqual("15", result.Day);
            Assert.AreEqual(0, result.Hour.Length);
        }

        [TestMethod]
        public void ParseTemplateFileNameReturnsPrefixWhenFedNameWithJustTxtSuffix()
        {
            const string Name = "NAME_A";

            TemplateParseResult result;

            // test
            result = Utility.ParseTemplateFileName(UtilityTests.ManifestFileNameRegexSet, Name + ".txt");

            // verify
            Assert.AreEqual(2, result.PatternIndex);
            Assert.AreEqual(0, result.Year.Length);
            Assert.AreEqual(0, result.Month.Length);
            Assert.AreEqual(0, result.Day.Length);
            Assert.AreEqual(0, result.Hour.Length);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        // CollectionHash

        [TestMethod]
        public void CollectionHashReturnsSameValueWhenTwoCollectionsHaveTheSameElementsInTheSameOrders()
        {
            const string Item1 = "ITEM1";
            const string Item2 = "THISISITEM2";
            const string Item3 = "WOWMOREITEMSSTILL";

            ICollection<string> list1 = new[] { Item1, Item2, Item3 };
            ICollection<string> list2 = new[] { Item1, Item2, Item3 };

            // test
            int hash1 = Utility.GetHashCodeForUnorderedCollection(list1);
            int hash2 = Utility.GetHashCodeForUnorderedCollection(list2);

            // verify
            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod]
        public void CollectionHashReturnsSameValueWhenTwoCollectionsHaveTheSameElementsInDifferentOrders()
        {
            const string Item1 = "ITEM1";
            const string Item2 = "THISISITEM2";
            const string Item3 = "WOWMOREITEMSSTILL";

            ICollection<string> list1 = new[] { Item1, Item2, Item3 };
            ICollection<string> list2 = new[] { Item2, Item1, Item3 };

            // test
            int hash1 = Utility.GetHashCodeForUnorderedCollection(list1);
            int hash2 = Utility.GetHashCodeForUnorderedCollection(list2);

            // verify
            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod]
        public void CollectionHashReturnsDifferentValueWhenTwoCollectionsHaveDifferentElementsInDifferentOrders()
        {
            const string Item1 = "ITEM1";
            const string Item2 = "THISISITEM2";
            const string Item3 = "WOWMOREITEMSSTILL";

            ICollection<string> list1 = new[] { Item1, Item1, Item3 };
            ICollection<string> list2 = new[] { Item2, Item3, Item2 };

            // test
            int hash1 = Utility.GetHashCodeForUnorderedCollection(list1);
            int hash2 = Utility.GetHashCodeForUnorderedCollection(list2);

            // verify
            Assert.AreNotEqual(hash1, hash2);
        }

    }
}