namespace Microsoft.PrivacyServices.Testing
{
    using Ploeh.AutoFixture.Xunit2;

    /// <summary>
    /// This adds a new attribute that will provide AutoFixture <c>Moq</c> support 
    /// that also provides defaults implementation for methods and properties.
    /// This works for xUnit2 tests only.
    /// </summary>
    public class InlineAutoMoqDataAttribute : InlineAutoDataAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InlineAutoMoqDataAttribute" /> class.
        /// </summary>
        /// <param name="values">The InlineData that would normally be used for xUnit.</param>
        public InlineAutoMoqDataAttribute(params object[] values)
            : base(new AutoMoqDataAttribute(), values)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InlineAutoMoqDataAttribute" /> class.
        /// </summary>
        /// <param name="baseAttribute">A base attribute that has customizations for data generation.</param>
        /// <param name="values">The InlineData that would normally be used for xUnit.</param>
        protected InlineAutoMoqDataAttribute(AutoMoqDataAttribute baseAttribute, params object[] values)
            : base(baseAttribute, values)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether recursion check should be disabled or not.
        /// </summary>
        public bool DisableRecursionCheck
        {
            get { return false; }
            set { this.AutoDataAttribute.Fixture.DisableRecursionCheck(); }
        }
    }
}
