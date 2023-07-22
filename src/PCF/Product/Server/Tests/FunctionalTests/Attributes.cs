namespace PCF.FunctionalTests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Xunit2;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Method)]
    public class AutoMoqDataAttribute : AutoDataAttribute
    { 
        public AutoMoqDataAttribute() : base(new Fixture().Customize(new AutoMoqCustomization()))
        {
            this.Fixture.Customize<TimeRangePredicate>(composer => 
                composer.Without(trp => trp.StartTime)
                        .Without(trp => trp.EndTime)
                        .Do(trp => 
                        {
                            trp.StartTime = this.Fixture.Create<DateTimeOffset>();
                            trp.EndTime = trp.StartTime.AddDays(100);
                        }));
        }
    }

    public abstract class AutoMoqCommandAttribute : AutoMoqDataAttribute
    {
        public string AssetGroupId { get; set; } = Guid.NewGuid().ToString("n");

        public Type SubjectType { get; set; } = typeof(MsaSubject);

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "test code")]
        protected AutoMoqCommandAttribute()
        {
            this.Fixture.Customize(this.CreateCustomization());
        }

        protected abstract ICustomization CreateCustomization();
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AutoMoqDeleteCommandAttribute : AutoMoqCommandAttribute
    {
        public bool IncludeDataTypePredicate { get; set; } = true;

        public string DataTypeId { get; set; } = "BrowsingHistory";

        protected override ICustomization CreateCustomization()
        {
            return new DeleteCustomization(this);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AutoMoqExportCommandAttribute : AutoMoqCommandAttribute
    {
        public string[] DataTypeIds { get; set; }

        public AutoMoqExportCommandAttribute(params string[] dataTypes)
        {
            if (dataTypes.Length == 0)
            {
                this.DataTypeIds = new[] { "BrowsingHistory", "ContentConsumption", "SearchRequestsAndQuery" };
            }
            else
            {
                this.DataTypeIds = dataTypes;
            }
        }

        protected override ICustomization CreateCustomization()
        {
            return new ExportCustomization(this);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AutoMoqAccountCloseCommandAttribute : AutoMoqCommandAttribute
    {
        public TenantIdType TenantIdType { get; set; } = TenantIdType.Home;

        protected override ICustomization CreateCustomization()
        {
            return new AccountCloseCustomization(this);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AutoMoqAgeOutCommandAttribute : AutoMoqCommandAttribute
    {
        protected override ICustomization CreateCustomization()
        {
            return new AgeOutCustomization(this);
        }
    }
}
