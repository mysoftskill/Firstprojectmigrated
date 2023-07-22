namespace PCF.UnitTests
{
    using System;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Kernel;

    public static class Extensions
    {
        public static object Create(this IFixture fixture, Type type)
        {
            return new SpecimenContext(fixture).Resolve(type);
        }
    }
}
