namespace Microsoft.PrivacyServices.DataManagement.Client.Filters.UnitTest
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    using Xunit;

    public class FilterRequestStringTest
    {
        [Theory(DisplayName = "Verify filter request string extension And function.")]
        [InlineData("contains(name, 'Value')", "name eq 'Value'", "contains(name, 'Value') and name eq 'Value'")]
        [InlineData("contains(name, 'Value')", null, "contains(name, 'Value')")]
        [InlineData("contains(name, 'Value')", "", "contains(name, 'Value')")]
        [InlineData(null, "name eq 'Value'", "name eq 'Value'")]
        [InlineData("", "name eq 'Value'", "name eq 'Value'")]
        [InlineData("", "", "")]
        [InlineData("", null, "")]
        [InlineData(null, "", "")]
        [InlineData(null, null, "")]
        public void VerifyFilterRequestStringAndFunction(string filterStr1, string filterStr2, string expected)
        {
            var actual = filterStr1.And(filterStr2);
            Assert.Equal(expected, actual);
        }
    }
}