namespace PCF.UnitTests
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using SemanticComparison.Fluent;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class LeaseReceiptTests : INeedDataBuilders
    {
        [Fact]
        public void CanSerializeAndDeserialize()
        {
            LeaseReceipt leaseReceipt = this.ALeaseReceipt();

            string json = leaseReceipt.Serialize();
            LeaseReceipt parsed = LeaseReceipt.Parse(json);

            leaseReceipt.AsSource().OfLikeness<LeaseReceipt>().ShouldEqual(parsed);
        }

        [Fact]
        public void ParseNullThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LeaseReceipt.Parse(null));
        }

        [Fact]
        public void ParseEmptyThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LeaseReceipt.Parse(string.Empty));
        }

        [Fact]
        public void TryParseNull()
        {
            Assert.False(LeaseReceipt.TryParse(null, out LeaseReceipt leaseReceipt));
        }

        [Fact]
        public void TryParseEmpty()
        {
            Assert.False(LeaseReceipt.TryParse(string.Empty, out LeaseReceipt leaseReceipt));
        }
    }
}
