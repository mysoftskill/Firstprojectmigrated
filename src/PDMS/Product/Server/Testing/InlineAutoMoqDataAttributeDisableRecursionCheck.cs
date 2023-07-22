namespace Microsoft.PrivacyServices.Testing
{
    using Ploeh.AutoFixture.Xunit2;

    /// <summary>
    /// This adds a new attribute that will provide AutoFixture <c>Moq</c> support 
    /// that also provides defaults implementation for methods and properties.
    /// This works for xUnit2 tests only.
    /// </summary>
    public class InlineAutoMoqDataAttributeDisableRecursionCheck : InlineAutoDataAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InlineAutoMoqDataAttribute" /> class.
        /// </summary>
        /// <param name="baseAttribute">A base attribute that has customizations for data generation.</param>
        /// <param name="values">The InlineData that would normally be used for xUnit.</param>
        public InlineAutoMoqDataAttributeDisableRecursionCheck(params object[] values)
            : base(new AutoMoqDataAttribute(true), values)
        {
        }
    }
}
