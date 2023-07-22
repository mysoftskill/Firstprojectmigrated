// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Utility.Model
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json.Linq;

    [TestClass]
    public class JsonNetModelManipulatorTests
    {
        private class ComplexType
        {
            public ComplexType(string d1, string d2) { this.Data1 = d1; this.Data2 = d2; }
            public ComplexType() {  }
            public string Data1 { get; set; }
            public string Data2 { get; set; }
        }

        private class ComplexType2
        {
            public ComplexType2(string d1, string d3) { this.Data1 = d1; this.Data3 = d3; }
            public ComplexType2() { }
            public string Data1 { get; set; }
            public string Data3 { get; set; }
        }

        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private readonly JsonNetModelManipulator testObj = new JsonNetModelManipulator();

        private IContext ctx;

        [TestInitialize]
        public void TestInit()
        {
            this.ctx = this.mockCtx.Object;
        }
        
        [TestMethod]
        public void CreateEmptyCreatesAnEmptyJObject()
        {
            object result = this.testObj.CreateEmpty();

            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.AreEqual(0, ((JObject)result).Count);
        }

        [TestMethod]
        public void TransformFromConvertObjectIntoJToken()
        {
            object result = this.testObj.TransformFrom(new object());

            Assert.IsInstanceOfType(result, typeof(JToken));
            Assert.AreEqual(JTokenType.Object, ((JToken)result).Type);
        }

        [TestMethod]
        public void TransformFromConvertMakesNoChangesToInputThatIsAlreadyCorrect()
        {
            object input = new JObject();

            object result = this.testObj.TransformFrom(input);

            Assert.AreSame(input, result);
        }

        [TestMethod]
        public void TransformToReturnsDefaultValueIfInputNull()
        {
            ComplexType valComplex;

            valComplex = this.testObj.TransformTo<ComplexType>(null);

            Assert.AreEqual(default(ComplexType), valComplex);
        }

        [TestMethod]
        public void TransformToReturnsSameObjectIfAlreadyIsInstanceOfObject()
        {
            ComplexType valComplex;
            ComplexType expected = new ComplexType();

            // test
            valComplex = this.testObj.TransformTo<ComplexType>(expected);

            // validate
            Assert.AreSame(expected, valComplex);
        }

        [TestMethod]
        public void TransformToReturnsObjectIfGivenJToken()
        {
            JObject input =
                new JObject(
                    new JProperty("Data1", "val1"),
                    new JProperty("Data2", "val2"));

            ComplexType valComplex;

            // test
            valComplex = this.testObj.TransformTo<ComplexType>(input);

            // validate
            Assert.AreEqual("val1", valComplex.Data1);
            Assert.AreEqual("val2", valComplex.Data2);
        }

        [TestMethod]
        public void TransformToReturnsObjectIfGivenObjectWithSimilarProperties()
        {
            ComplexType2 valComplex2 = new ComplexType2 { Data1 = "2-val1", Data3 = "2-val3" };

            ComplexType valComplex;

            // test
            valComplex = this.testObj.TransformTo<ComplexType>(valComplex2);

            // validate
            Assert.AreEqual("2-val1", valComplex.Data1);
            Assert.IsNull(valComplex.Data2);
        }

        [TestMethod]
        public void ExtractReturnsResultWhenSimpleTypeValueFound()
        {
            JObject model = new JObject(new JProperty("value", 1));

            bool result;

            result = this.testObj.TryExtractValue(this.ctx, model, "value", 0, out int intVal);
            Assert.IsTrue(result);
            Assert.AreEqual(1, intVal);

            result = this.testObj.TryExtractValue(this.ctx, model, "value", 0L, out long longVal);
            Assert.IsTrue(result);
            Assert.AreEqual(1L, longVal);

            result = this.testObj.TryExtractValue(this.ctx, model, "value", 0.0, out double dblVal);
            Assert.IsTrue(result);
            Assert.AreEqual(1.0d, dblVal);

            result = this.testObj.TryExtractValue(this.ctx, model, "value", null, out string strVal);
            Assert.IsTrue(result);
            Assert.AreEqual("1", strVal);
        }

        [TestMethod]
        public void ExtractReturnsResultWhenComplexTypeValueFound()
        {
            JObject model = 
                new JObject(
                    new JProperty(
                        "value", 
                        new JObject(
                            new JProperty("Data1", "val1"),
                            new JProperty("Data2", "val2"))));

            bool result;

            // test
            result = this.testObj.TryExtractValue(this.ctx, model, "value", null, out ComplexType complexVal);

            // validate
            Assert.IsTrue(result);
            Assert.AreEqual("val1", complexVal.Data1);
            Assert.AreEqual("val2", complexVal.Data2);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPathException))]
        public void ExtractThrowsWhenPathIsNotASingleElementQuery()
        {
            JObject model =
                new JObject(
                    new JProperty("value1", new JObject(new JProperty("data", "val1"))),
                    new JProperty("value2", new JObject(new JProperty("data", "val2"))));

            this.testObj.TryExtractValue(this.ctx, model, "$..data", null, out string[] _);
        }

        [TestMethod]
        public void ExtractReturnsDefaultWhenNoValueFound()
        {
            JObject model = new JObject(new JProperty("value", 1));

            bool result;

            result = this.testObj.TryExtractValue(this.ctx, model, "notFound", 0, out int intVal);
            Assert.IsFalse(result);
            Assert.AreEqual(0, intVal);

            result = this.testObj.TryExtractValue(this.ctx, model, "notFound", 0L, out double longVal);
            Assert.IsFalse(result);
            Assert.AreEqual(0L, longVal);

            result = this.testObj.TryExtractValue(this.ctx, model, "notFound", 0.0, out double dblVal);
            Assert.IsFalse(result);
            Assert.AreEqual(0.0d, dblVal);

            result = this.testObj.TryExtractValue(this.ctx, model, "notFound", null, out string strVal);
            Assert.IsFalse(result);
            Assert.AreEqual(null, strVal);
        }
        
        [TestMethod]
        public void RemoveCorrectlyRemovesPropertyFromObjectForSimplePath()
        {
            JObject model =
                new JObject(
                    new JProperty(
                        "value",
                        new JObject(
                            new JProperty("Data1", "val1"),
                            new JProperty("Data2", "val2"))));

            JObject obj2;

            // test
            obj2 = (JObject)this.testObj.RemoveSubmodel(model, "value");

            // validate
            Assert.AreEqual(0, obj2.Count);
            Assert.AreEqual(0, model.Count);
        }

        [TestMethod]
        public void RemoveCorrectlyRemovesPropertyFromObjectForQuotedOneElementPath()
        {
            JObject model =
                new JObject(
                    new JProperty(
                        "value",
                        new JObject(
                            new JProperty("Data1", "val1"),
                            new JProperty("Data2", "val2"))));

            JObject obj2;

            // test
            obj2 = (JObject)this.testObj.RemoveSubmodel(model, @"""value""");

            // validate
            Assert.AreEqual(0, obj2.Count);
            Assert.AreEqual(0, model.Count);
        }

        [TestMethod]
        public void RemoveCorrectlyRemovesPropertyFromObjectForComplexPath()
        {
            JObject model =
                new JObject(
                    new JProperty(
                        "value",
                        new JObject(
                            new JProperty(
                                "inner",
                                new JObject(
                                    new JProperty("Data1", "val1"),
                                    new JProperty("Data2", "val2"))))));

            JObject obj2;

            // test
            obj2 = (JObject)this.testObj.RemoveSubmodel(model, @"value.inner");

            // validate
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual(0, ((JObject)(obj2.Property("value").Value)).Count);
        }

        [TestMethod]
        public void RemoveDoesNothingIfSimplePathDoesNotExist()
        {
            JObject model =
                new JObject(
                    new JProperty(
                        "value",
                        new JObject(
                            new JProperty("Data1", "val1"),
                            new JProperty("Data2", "val2"))));

            JObject obj2;

            // test
            obj2 = (JObject)this.testObj.RemoveSubmodel(model, @"notExist");

            // validate
            Assert.AreEqual(1, obj2.Count);
            Assert.AreEqual(1, model.Count);
        }

        [TestMethod]
        public void RemoveDoesNothingIfComplexPathDoesNotExist()
        {
            JObject model =
                new JObject(
                    new JProperty(
                        "value",
                        new JObject(
                            new JProperty(
                                "inner",
                                new JObject(
                                    new JProperty("Data1", "val1"),
                                    new JProperty("Data2", "val2"))))));

            JObject obj2;

            // test
            obj2 = (JObject)this.testObj.RemoveSubmodel(model, @"notExist.stillNotExist");

            // validate
            Assert.AreEqual(1, model.Count);
            Assert.IsNotNull(obj2.SelectToken("value.inner.Data1"));
            Assert.IsNotNull(obj2.SelectToken("value.inner.Data2"));
        }

        [TestMethod]
        public void AddSubmodelAddsSimpleValueToTargetWhenNoCurrentValueAndReplaceExistingMode()
        {
            const string Prop = "test";
            const int Expected = 1;

            JObject model = new JObject();

            int value;

            // test
            this.testObj.AddSubmodel(this.mockCtx.Object, model, Prop, Expected, MergeMode.ReplaceExisting);

            // validate
            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, model, Prop, 0, out value));
            Assert.AreEqual(Expected, value);
        }

        [TestMethod]
        public void AddSubmodelAddsArrayValueToTargetWhenNoCurrentValueAndReplaceExistingMode()
        {
            const string Prop = "test";
            const int Expected = 1;

            JObject model = new JObject();
            JArray newVal = new JArray(Expected);

            JArray value;

            // test
            this.testObj.AddSubmodel(this.mockCtx.Object, model, Prop, newVal, MergeMode.ReplaceExisting);

            // validate
            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, model, Prop, null, out value));
            Assert.AreEqual(newVal.Count, value.Count);
            Assert.AreEqual(newVal[0], value[0]);
        }

        [TestMethod]
        public void AddSubmodelOverwritesExistingValueInTargetWhenReplaceExistingMode()
        {
            const string Prop = "test";
            const int Expected = 1;

            JObject model = new JObject(new JProperty(Prop, new[] { Expected + 5 }));

            int value;

            // test
            this.testObj.AddSubmodel(this.mockCtx.Object, model, Prop, Expected, MergeMode.ReplaceExisting);

            // validate
            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, model, Prop, 0, out value));
            Assert.AreEqual(Expected, value);
        }

        private static IEnumerable<object[]> AddSubmodelAddsValueToTargetAsArrayWhenNoCurrentValueArgs =>
            new List<object[]>
            {
                new object[] { 1, MergeMode.ArrayAdd, 1 },
                new object[] { 1, MergeMode.ArrayUnion, 1 },
                new object[] { new JArray(1), MergeMode.ArrayUnion, 1 },
            };

        [TestMethod]
        [DynamicData(nameof(JsonNetModelManipulatorTests.AddSubmodelAddsValueToTargetAsArrayWhenNoCurrentValueArgs))]
        public void AddSubmodelAddsValueToTargetAsArrayWhenNoCurrentValueAndSpecifiedMode(
            object valueToAdd,
            MergeMode mode,
            int expected)
        {
            const string Prop = "test";

            JObject model = new JObject();

            JArray value;

            // test
            this.testObj.AddSubmodel(this.mockCtx.Object, model, Prop, valueToAdd, mode);

            // validate
            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, model, Prop, null, out value));
            Assert.AreEqual(1, value.Count);
            Assert.AreEqual(expected, value.First.ToObject<int>());
        }

        [TestMethod]
        public void AddSubmodelAddsArrayValueToTargetAsArrayWhenNoCurrentValueAndArrayAddMode()
        {
            const string Prop = "test";
            const int Expected = 1;

            JObject model = new JObject();
            JArray newVal = new JArray(Expected);

            JArray value;

            // test
            this.testObj.AddSubmodel(this.mockCtx.Object, model, Prop, newVal, MergeMode.ArrayAdd);

            // validate
            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, model, Prop, null, out value));
            Assert.AreEqual(1, value.Count);
            Assert.AreEqual(1, value.First.ToObject<JArray>().Count);
            Assert.AreEqual(1, value.First.ToObject<JArray>().First.ToObject<int>());
        }

        private static IEnumerable<object[]> AddSubmodelAddsValueToTargetAsArrayWhenCurrentSimpleValueArgs =>
            new List<object[]>
            {
                new object[] { 1, MergeMode.ArrayAdd, 1 },
                new object[] { 1, MergeMode.ArrayUnion, 1 },
                new object[] { new JArray(1), MergeMode.ArrayUnion, 1 },
            };

        [TestMethod]
        [DynamicData(nameof(JsonNetModelManipulatorTests.AddSubmodelAddsValueToTargetAsArrayWhenCurrentSimpleValueArgs))]
        public void AddSubmodelAddsValueToTargetAsArrayWhenCurrentSimpleValueAndSpecifiedMode(
            object valueToAdd,
            MergeMode mode,
            int expected)
        {
            const string Prop = "test";
            const int ExistingExpected = 10;

            JObject model = new JObject(new JProperty(Prop, ExistingExpected));

            JArray value;

            // test
            this.testObj.AddSubmodel(this.mockCtx.Object, model, Prop, valueToAdd, mode);

            // validate
            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, model, Prop, null, out value));
            Assert.AreEqual(2, value.Count);
            Assert.AreEqual(ExistingExpected, value[0].ToObject<int>());
            Assert.AreEqual(expected, value[1].ToObject<int>());
        }

        [TestMethod]
        public void AddSubmodelAddsArrayValueToTargetAsArrayWhenCurrentSimpleValueAndArrayAddMode()
        {
            const string Prop = "test";
            const int ExistingExpected = 10;
            const int NewExpected = 1;

            // need the cast to JToken to force using the non-JArray constructor which copies in the input array
            JObject model = new JObject(new JProperty(Prop, ExistingExpected));
            JArray newVal = new JArray(NewExpected);

            JArray value;

            // test
            this.testObj.AddSubmodel(this.mockCtx.Object, model, Prop, newVal, MergeMode.ArrayAdd);

            // validate
            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, model, Prop, null, out value));
            Assert.AreEqual(2, value.Count);
            Assert.AreEqual(ExistingExpected, value[0].ToObject<int>());
            Assert.AreEqual(1, value[1].ToObject<JArray>().Count);
            Assert.AreEqual(NewExpected, value[1].ToObject<JArray>().First.ToObject<int>());
        }

        private static IEnumerable<object[]> AddSubmodelAddsValueToTargetAsArrayWhenCurrentArrayValueArgs =>
            new List<object[]>
            {
                new object[] { 1, MergeMode.ArrayAdd, 1 },
                new object[] { 1, MergeMode.ArrayUnion, 1 },
                new object[] { new JArray(1), MergeMode.ArrayUnion, 1 },
            };

        [TestMethod]
        [DynamicData(nameof(JsonNetModelManipulatorTests.AddSubmodelAddsValueToTargetAsArrayWhenCurrentArrayValueArgs))]
        public void AddSubmodelAddsValueToTargetAsArrayWhenCurrentArrayValueAndSpecifiedMode(
            object valueToAdd,
            MergeMode mode,
            int expected)
        {
            const string Prop = "test";
            const int ExistingExpected = 10;

            JObject model = new JObject(new JProperty(Prop, new JArray(ExistingExpected)));

            JArray value;

            // test
            this.testObj.AddSubmodel(this.mockCtx.Object, model, Prop, valueToAdd, mode);

            // validate
            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, model, Prop, null, out value));
            Assert.AreEqual(2, value.Count);
            Assert.AreEqual(ExistingExpected, value[0].ToObject<int>());
            Assert.AreEqual(expected, value[1].ToObject<int>());
        }

        [TestMethod]
        public void AddSubmodelAddsArrayValueToTargetAsArrayWhenCurrentArrayValueAndArrayAddMode()
        {
            const string Prop = "test";
            const int ExistingExpected = 10;
            const int NewExpected = 10;

            // need the cast to JToken to force using the non-JArray constructor which copies in the input array
            JObject model = new JObject(new JProperty(Prop, new JArray((JToken)new JArray(ExistingExpected))));
            JArray newVal = new JArray(NewExpected);

            JArray value;

            // test
            this.testObj.AddSubmodel(this.mockCtx.Object, model, Prop, newVal, MergeMode.ArrayAdd);

            // validate
            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, model, Prop, null, out value));
            Assert.AreEqual(2, value.Count);
            Assert.AreEqual(1, value[0].ToObject<JArray>().Count);
            Assert.AreEqual(ExistingExpected, value[0].ToObject<JArray>().First.ToObject<int>());
            Assert.AreEqual(1, value[1].ToObject<JArray>().Count);
            Assert.AreEqual(ExistingExpected, value[1].ToObject<JArray>().First.ToObject<int>());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void MergeModelThrowsWhenSourceIsNotConvertibleToAJobject()
        {
            this.testObj.MergeModels<ModelValue>(this.mockCtx.Object, new JValue(1), null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void MergeModelThrowsWhenTargetIsNotConvertibleToAJobject()
        {
            this.testObj.MergeModels<ModelValue>(this.mockCtx.Object, null, new JValue(1), null);
        }

        [TestMethod]
        public void MergeModelCreatesAndReturnsNewObjectIfTargetIsNull()
        {
            object result;

            result = this.testObj.MergeModels<ModelValue>(this.mockCtx.Object, null, null, null);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.AreEqual(0, ((JObject)result).Count);
        }

        [TestMethod]
        public void MergeModelCreatesAndReturnsInputObjectIfTargetIsNotNullAndAJobject()
        {
            object input = new JObject();
            object result;

            result = this.testObj.MergeModels<ModelValue>(this.mockCtx.Object, null, input, null);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.AreEqual(0, ((JObject)result).Count);
        }

        [TestMethod]
        public void MergeModelExecutesSelectManyExpressionAndAddsToResult()
        {
            const string TargetPropName = "data";

            IDictionary<string, ModelValue> map = new Dictionary<string, ModelValue>
            {
                { TargetPropName, new ModelValue { SelectMany = "$..Data2" } }
            };

            // anonymous type- has to be var
            var input = new
            {
                Data = new[] { new ComplexType("v11", "v21"), new ComplexType("v12", "v22") },
            };

            JArray transformResult;
            object result;

            result = this.testObj.MergeModels(this.mockCtx.Object, input, null, map);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.AreEqual(1, ((JObject)result).Count);

            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, result, TargetPropName, null, out transformResult));
            Assert.AreEqual(2, transformResult.Count);
            Assert.AreEqual("v21", transformResult[0]);
            Assert.AreEqual("v22", transformResult[1]);
        }

        [TestMethod]
        public void MergeModelExecutesSelectManyExpressionAndAddsEmptyArrayIfNoDataFoundAndConstIsNull()
        {
            const string TargetPropName = "data";

            IDictionary<string, ModelValue> map = new Dictionary<string, ModelValue>
            {
                { TargetPropName, new ModelValue { SelectMany = "$..Data3" } }
            };

            // anonymous type- has to be var
            var input = new { SourceData = new[] { new ComplexType("v11", "v21"), new ComplexType("v12", "v22") } };

            JArray transformResult;
            object result;

            result = this.testObj.MergeModels(this.mockCtx.Object, input, null, map);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.AreEqual(1, ((JObject)result).Count);

            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, result, TargetPropName, null, out transformResult));
            Assert.AreEqual(0, transformResult.Count);
        }

        [TestMethod]
        public void MergeModelExecutesSelectManyExpressionAndAddsConstItemToResultIfNoDataFoundAndConstIsNotNull()
        {
            const string TargetPropName = "data";

            IDictionary<string, ModelValue> map = new Dictionary<string, ModelValue>
            {
                { TargetPropName, new ModelValue { SelectMany = "$..Data3", Const = new[] { "c1", "c2" } } }
            };

            // anonymous type- has to be var
            var input = new { SourceData = new[] { new ComplexType("v11", "v21"), new ComplexType("v12", "v22") } };

            JArray transformResult;
            object result;

            result = this.testObj.MergeModels(this.mockCtx.Object, input, null, map);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.AreEqual(1, ((JObject)result).Count);

            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, result, TargetPropName, null, out transformResult));
            Assert.AreEqual(2, transformResult.Count);
            Assert.AreEqual("c1", transformResult[0]);
            Assert.AreEqual("c2", transformResult[1]);
        }

        [TestMethod]
        public void MergeModelExecutesSelectExpressionAndAddsToResult()
        {
            const int SourceValue = 1;
            const string TargetPropName = "data";

            IDictionary<string, ModelValue> map = new Dictionary<string, ModelValue>
            {
                { TargetPropName, new ModelValue { Select = "SourceData" } }
            };

            // anonymous type- has to be var
            var input = new { SourceData = SourceValue };

            object result;
            int transformResult;

            result = this.testObj.MergeModels(this.mockCtx.Object, input, null, map);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.AreEqual(1, ((JObject)result).Count);

            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, result, TargetPropName, 0, out transformResult));
            Assert.AreEqual(SourceValue, transformResult);
        }

        [TestMethod]
        public void MergeModelExecutesSelectExpressionAndAddsNothingToResultIfNoValueExistsAndNoConstSpecified()
        {
            const int SourceValue = 1;
            const string TargetPropName = "data";

            IDictionary<string, ModelValue> map = new Dictionary<string, ModelValue>
            {
                { TargetPropName, new ModelValue { Select = "NotExist" } }
            };

            // anonymous type- has to be var
            var input = new { SourceData = SourceValue };

            object result;

            result = this.testObj.MergeModels(this.mockCtx.Object, input, null, map);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.AreEqual(0, ((JObject)result).Count);
        }

        [TestMethod]
        public void MergeModelExecutesSelectExpressionAndAddsConstToResultIfNoValueExistsButConstSpecified()
        {
            const int SourceValue = 1;
            const int ConstValue = 100;
            const string TargetPropName = "data";

            IDictionary<string, ModelValue> map = new Dictionary<string, ModelValue>
            {
                { TargetPropName, new ModelValue { Select = "NotExist", Const = ConstValue } }
            };

            // anonymous type- has to be var
            var input = new { SourceData = SourceValue };

            object result;
            int transformResult;

            result = this.testObj.MergeModels(this.mockCtx.Object, input, null, map);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.AreEqual(1, ((JObject)result).Count);

            Assert.IsTrue(this.testObj.TryExtractValue(this.ctx, result, TargetPropName, 0, out transformResult));
            Assert.AreEqual(ConstValue, transformResult);
        }
    }
}
