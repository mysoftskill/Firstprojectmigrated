// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.UnitTests.Utilities
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json.Linq;

    [TestClass]
    public class JsonObjectConverterTests
    {
        [TestMethod]
        public void DeserializeParsesJsonObjectWithSimpleTypesToDictionary()
        {
            const string Json = "{ longVal: 1, stringVal: \"string\", doubleVal: 1.1 }";

            // test
            IDictionary<string, object> result = JsonObjectConverter.Deserialize<IDictionary<string, object>>(Json);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(1, (long)result["longVal"]);
            Assert.AreEqual("string", (string)result["stringVal"]);
            Assert.AreEqual(1.1, (double)result["doubleVal"]);
        }

        [TestMethod]
        public void DeserializeParsesJsonObjectWithExtendedDateTypeToDictionaryContainingADate()
        {
            const string Json = "{ dateVal: \"{datetime'2018-01-02 03:04:05Z'}\" }";

            // test
            IDictionary<string, object> result = 
                JsonObjectConverter.Deserialize<IDictionary<string, object>>(Json, JsonConvertFlags.UseDateTimeSyntax);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(new DateTimeOffset(2018, 01, 02, 03, 04, 05, TimeSpan.Zero), (DateTimeOffset)result["dateVal"]);
        }

        [TestMethod]
        public void DeserializeParsesJsonObjectWithExtendedDateTypeButNoFlagToDictionaryContainingAString()
        {
            const string Json = "{ dateVal: \"{datetime'2018-01-02 03:04:05Z'}\" }";

            // test
            IDictionary<string, object> result =
                JsonObjectConverter.Deserialize<IDictionary<string, object>>(Json);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual("{datetime'2018-01-02 03:04:05Z'}", (string)result["dateVal"]);
        }

        [TestMethod]
        public void DeserializeParsesJsonObjectWithComplexTypesToDictionaryContainingADictionary()
        {
            const string Json = "{ inner: { data1: 1 } }";

            IDictionary<string, object> result;
            IDictionary<string, object> inner;

            // test
            result = JsonObjectConverter.Deserialize<IDictionary<string, object>>(Json);

            // verify
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result["inner"], typeof(IDictionary<string, object>));
            inner = (IDictionary<string, object>)result["inner"];
            Assert.AreEqual(1, inner.Count);
            Assert.AreEqual(1, (long)inner["data1"]);
        }

        [TestMethod]
        public void DeserializeParsesJsonObjectWithArrayOfIntToDictionaryContainingAListOfInts()
        {
            const string Json = "{ inner: [ 1 ] }";

            IDictionary<string, object> result;
            IList<object> inner;

            // test
            result = JsonObjectConverter.Deserialize<IDictionary<string, object>>(Json);

            // verify
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result["inner"], typeof(IList<object>));
            inner = (IList<object>)result["inner"];
            Assert.AreEqual(1, inner.Count);
            Assert.AreEqual(1, (long)inner[0]);
        }

        [TestMethod]
        public void DeserializeParsesJsonObjectWithArrayOfObjectsToDictionaryContainingAListOfObjects()
        {
            const string Json = "{ inner: { data1: [ 1 ] } }";

            IDictionary<string, object> result;
            IDictionary<string, object> innerObj;
            IList<object> innerList;

            // test
            result = JsonObjectConverter.Deserialize<IDictionary<string, object>>(Json);

            // verify
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result["inner"], typeof(IDictionary<string, object>));

            innerObj = (IDictionary<string, object>)result["inner"];
            Assert.AreEqual(1, innerObj.Count);
            Assert.IsInstanceOfType(innerObj["data1"], typeof(IList<object>));

            innerList = (IList<object>)innerObj["data1"];
            Assert.AreEqual(1, innerList.Count);
            Assert.AreEqual(1, (long)innerList[0]);
        }
        
        [TestMethod]
        public void DeserializeParsesJsonArrayOfIntsIntoListOfInts()
        {
            const string Json = "[ 1 ]";

            IList<object> result;

            // test
            result = JsonObjectConverter.Deserialize<IList<object>>(Json);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, (long)result[0]);
        }

        [TestMethod]
        public void DeserializeParsesJsonArrayOfObjectsToListOfDictionaries()
        {
            const string Json = "[ { data1: 1 } ]";

            IDictionary<string, object> innerObj;
            IList<object> result;

            // test
            result = JsonObjectConverter.Deserialize<IList<object>>(Json);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOfType(result[0], typeof(IDictionary<string, object>));

            innerObj = (IDictionary<string, object>)result[0];
            Assert.AreEqual(1, innerObj.Count);
            Assert.AreEqual(1, (long)innerObj["data1"]);
        }

        [TestMethod]
        public void ToObjectConvertsJObjectToDictionary()
        {
            DateTimeOffset time = new DateTimeOffset(2018, 01, 02, 03, 04, 05, TimeSpan.Zero);

            JObject obj = new JObject
            {
                { "longVal", new JValue(1L) },
                { "stringVal", new JValue("string") },
                { "doubleVal", new JValue(1.1d) },
            };

            IDictionary<string, object> result;

            // test
            result = JsonObjectConverter.ToObject<IDictionary<string, object>>(obj);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(1, (long)result["longVal"]);
            Assert.AreEqual("string", (string)result["stringVal"]);
            Assert.AreEqual(1.1, (double)result["doubleVal"]);
        }

        [TestMethod]
        public void ToObjectConvertsJsonObjectWithExtendedDateTypeToDictionaryContainingADate()
        {
            JObject obj = new JObject
            {
                { "dateVal", new JValue("{datetime'2018-01-02 03:04:05Z'}") }
            };

            // test
            IDictionary<string, object> result =
                JsonObjectConverter.ToObject<IDictionary<string, object>>(obj, JsonConvertFlags.UseDateTimeSyntax);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(new DateTimeOffset(2018, 01, 02, 03, 04, 05, TimeSpan.Zero), (DateTimeOffset)result["dateVal"]);
        }

        [TestMethod]
        public void ToObjectConvertsJsonObjectWithExtendedDateTypeButNoFlagToDictionaryContainingAString()
        {
            JObject obj = new JObject
            {
                { "dateVal", new JValue("{datetime'2018-01-02 03:04:05Z'}") }
            };

            // test
            IDictionary<string, object> result =
                JsonObjectConverter.ToObject<IDictionary<string, object>>(obj);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual("{datetime'2018-01-02 03:04:05Z'}", (string)result["dateVal"]);
        }

        [TestMethod]
        public void ToObjectConvertsJsonObjectWithComplexTypesToDictionaryContainingADictionary()
        {
            JObject obj = new JObject
            {
                { "inner", new JObject { { "data1", new JValue(1L) } } },
            };

            IDictionary<string, object> result;
            IDictionary<string, object> inner;

            // test
            result = JsonObjectConverter.ToObject<IDictionary<string, object>>(obj);

            // verify
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result["inner"], typeof(IDictionary<string, object>));
            inner = (IDictionary<string, object>)result["inner"];
            Assert.AreEqual(1, inner.Count);
            Assert.AreEqual(1, (long)inner["data1"]);
        }

        [TestMethod]
        public void ToObjectConvertsJsonObjectWithArrayOfIntToDictionaryContainingAListOfInts()
        {
            JObject obj = new JObject
            {
                { "inner", new JArray { new JValue(1L) } },
            };

            IDictionary<string, object> result;
            IList<object> inner;

            // test
            result = JsonObjectConverter.ToObject<IDictionary<string, object>>(obj);

            // verify
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result["inner"], typeof(IList<object>));
            inner = (IList<object>)result["inner"];
            Assert.AreEqual(1, inner.Count);
            Assert.AreEqual(1, (long)inner[0]);
        }

        [TestMethod]
        public void ToObjectConvertsJsonObjectWithArrayOfObjectsToDictionaryContainingAListOfObjects()
        {
            JObject obj = new JObject
            {
                { "inner", new JObject { { "data1", new JArray { new JValue(1L) } } } },
            };

            IDictionary<string, object> result;
            IDictionary<string, object> innerObj;
            IList<object> innerList;

            // test
            result = JsonObjectConverter.ToObject<IDictionary<string, object>>(obj);

            // verify
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result["inner"], typeof(IDictionary<string, object>));

            innerObj = (IDictionary<string, object>)result["inner"];
            Assert.AreEqual(1, innerObj.Count);
            Assert.IsInstanceOfType(innerObj["data1"], typeof(IList<object>));

            innerList = (IList<object>)innerObj["data1"];
            Assert.AreEqual(1, innerList.Count);
            Assert.AreEqual(1, (long)innerList[0]);
        }

        [TestMethod]
        public void ToObjectConvertsJsonArrayOfIntsIntoListOfInts()
        {
            JArray obj = new JArray { new JValue(1L) };

            IList<object> result;

            // test
            result = JsonObjectConverter.ToObject<IList<object>>(obj);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, (long)result[0]);
        }

        [TestMethod]
        public void ToObjectConvertsJsonArrayOfObjectsToListOfDictionaries()
        {
            JArray obj = new JArray
            {
                new JObject { { "data1", new JValue(1L) } },
            };

            IDictionary<string, object> innerObj;
            IList<object> result;

            // test
            result = JsonObjectConverter.ToObject<IList<object>>(obj);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOfType(result[0], typeof(IDictionary<string, object>));

            innerObj = (IDictionary<string, object>)result[0];
            Assert.AreEqual(1, innerObj.Count);
            Assert.AreEqual(1, (long)innerObj["data1"]);
        }
    }
}
