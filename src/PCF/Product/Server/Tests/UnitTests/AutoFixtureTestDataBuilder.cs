using Ploeh.AutoFixture;
using System;

namespace PCF.UnitTests
{
    /// <summary>
    /// A test data builder that uses AutoMoq.
    /// </summary>
    public class AutoFixtureTestDataBuilder<T> : TestDataBuilder<T>
    {
        private readonly Fixture fixture = new Fixture();

        protected override T CreateNewObject()
        {
            return this.fixture.Create<T>();
        }

        public AutoFixtureTestDataBuilder<T> Register<TType>(Func<TType> factory)
        {
            this.fixture.Register(factory);
            return this;
        }
    }
}