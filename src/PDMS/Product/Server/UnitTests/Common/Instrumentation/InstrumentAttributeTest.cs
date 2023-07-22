namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using Xunit;

    public class InstrumentAttributeTest
    {
        [Fact(DisplayName = "When a method is decorated with Incoming, then extract that attribute information.")]
        [Incoming]
        public void VerifyIncomingAttributeWithoutName()
        {
            var attribute = InstrumentAttributeModule.GetAttribute(typeof(InstrumentAttributeTest), "VerifyIncomingAttributeWithoutName");
            Assert.Null(attribute.Name);
            Assert.True(attribute is IncomingAttribute);
        }

        [Fact(DisplayName = "When the InstrumentAttribute has a name, then set that value.")]
        [Incoming(Name = "Api")]
        public void VerifyIncomingAttributeWithName()
        {
            var attribute = InstrumentAttributeModule.GetAttribute(typeof(InstrumentAttributeTest), "VerifyIncomingAttributeWithName");
            Assert.Equal("Api", attribute.Name);
            Assert.True(attribute is IncomingAttribute);
        }

        [Fact(DisplayName = "When a method is decorated with Internal, then extract that attribute information.")]
        [Internal]
        public void VerifyInternalAttribute()
        {
            var attribute = InstrumentAttributeModule.GetAttribute(typeof(InstrumentAttributeTest), "VerifyInternalAttribute");
            Assert.True(attribute is InternalAttribute);
        }

        [Fact(DisplayName = "When a method is decorated with Outgoing, then extract that attribute information.")]
        [Outgoing]
        public void VerifyOutgoingAttribute()
        {
            var attribute = InstrumentAttributeModule.GetAttribute(typeof(InstrumentAttributeTest), "VerifyOutgoingAttribute");
            Assert.True(attribute is OutgoingAttribute);
        }
    }
}
