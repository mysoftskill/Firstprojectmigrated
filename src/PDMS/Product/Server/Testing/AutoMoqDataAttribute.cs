namespace Microsoft.PrivacyServices.Testing
{
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;

    /// <summary>
    /// This adds a new attribute that will provide AutoFixture <c>Moq</c> support 
    /// that also provides defaults implementation for methods and properties.
    /// This works for xUnit2 tests only.
    /// </summary>
    /// <remarks>
    /// <c>
    /// Interfaces with readonly properties will not work with newer versions of Moq.
    /// This is due to a regression in the Moq library. Until the bug is fixed,
    /// we must remain on Moq version 4.2.1409.1722.
    /// See https://github.com/Moq/moq4/issues/196 and http://stackoverflow.com/questions/32067727/autoconfiguredmoqcustomization-and-unsettable-properties.
    /// </c>
    /// </remarks>
    public class AutoMoqDataAttribute : AutoDataAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoMoqDataAttribute" /> class.
        /// </summary>
        /// <param name="disableRecursionCheck">Indicates if recursion check should be disabled or not.</param>
        public AutoMoqDataAttribute(bool disableRecursionCheck = false)
            : base(new Fixture().EnableAutoMoq())
        {
            if (disableRecursionCheck)
            {
                this.Fixture.DisableRecursionCheck();
            }

            this.Fixture.EnablePolicy();
            this.Fixture.EnableIdentity();
        }

        /// <summary>
        /// Gets or sets a value indicating whether recursion check should be disabled or not.
        /// </summary>
        public bool DisableRecursionCheck
        {
            get { return false; }
            set { this.Fixture.DisableRecursionCheck(); }
        }
    }
}
