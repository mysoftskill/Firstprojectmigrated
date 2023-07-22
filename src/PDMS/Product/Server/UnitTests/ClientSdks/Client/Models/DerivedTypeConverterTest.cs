namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using Newtonsoft.Json;

    using Xunit;

    public class DerivedTypeConverterTest 
    {
        [Fact(DisplayName = "When deserializing an abstract type, then use correct derived type.")]
        public void When_Deserialized_Then_CreateClassAsDerivedType()
        {
            var obj = JsonConvert.DeserializeObject<Base>("{ \"@odata.type\":\"prefix.Derived\"}");
            Assert.IsType<Derived>(obj);
        }

        [Fact(DisplayName = "When deserializing null, then do not fail.")]
        public void When_DeserializeNull_Then_ReturnNull()
        {
            var obj = JsonConvert.DeserializeObject<Base>(string.Empty);
            Assert.Null(obj);
        }

        [Fact(DisplayName = "When serializing an abstract type, then do not use the converter.")]
        public void When_Serialized_Then_DoNotUseTheConverter()
        {
            var str = JsonConvert.SerializeObject(new Derived() as Base);
            Assert.Equal("{}", str);
        }

        [Fact(DisplayName = "When deserializing a null derived property, then do not fail.")]
        public void When_DeserializeNullProperty_Then_ReturnNull()
        {
            var obj = JsonConvert.DeserializeObject<Container>("{\"Property\":null}");
            Assert.NotNull(obj);
            Assert.Null(obj.Property);
        }

        [Fact(DisplayName = "When deserializing a missing derived property, then do not fail.")]
        public void When_DeserializeMissingProperty_Then_ReturnNull()
        {
            var obj = JsonConvert.DeserializeObject<Container>("{}");
            Assert.NotNull(obj);
            Assert.Null(obj.Property);
        }

        [JsonConverter(typeof(DerivedTypeConverter), "prefix")]
        [DerivedType(typeof(Derived))]
        public abstract class Base
        {
        }

        public class Derived : Base
        {
        }

        public class Container
        {
            public Derived Property { get; set; }
        }
    }
}