namespace PCF.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Dsl;

    public class CommandCustomization<TCommand, TAttribute> : ICustomization
        where TCommand : PrivacyCommand
        where TAttribute : AutoMoqCommandAttribute
    {
        public CommandCustomization(TAttribute attribute)
        {
            this.Attribute = attribute;
        }

        protected TAttribute Attribute { get; set; }

        public void Customize(IFixture fixture)
        {
            fixture.Customize<TCommand>(composer => this.OnPreCustomize(composer).Do(this.OnPostCustomize));
        }

        protected virtual IPostprocessComposer<TCommand> OnPreCustomize(ICustomizationComposer<TCommand> composer)
        {
            return composer.Without(c => c.AgentState)
                           .Without(c => c.Subject)
                           .Without(d => d.ApproximateLeaseExpiration)
                           .Without(d => d.Timestamp)
                           .Without(d => d.AssetGroupId)
                           .Without(d => d.CommandId)
                           .Without(d => d.Verifier)
                           .Without(d => d.RequestBatchId)
                           .Without(d => d.ApplicableVariants)
                           .Without(d => d.CloudInstance);
        }

        protected virtual void OnPostCustomize(TCommand command)
        {
            command.Subject = (IPrivacySubject)this.Attribute.Fixture.Create(this.Attribute.SubjectType);
            command.ApproximateLeaseExpiration = DateTimeOffset.UtcNow.AddMinutes(-1);
            command.Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5);
            command.AssetGroupId = this.Attribute.AssetGroupId;
            command.CommandId = Guid.NewGuid().ToString("n");
            command.CloudInstance = "Public";
            command.RequestBatchId = Guid.NewGuid().ToString("n");
            command.Verifier = string.Empty;
            command.ApplicableVariants = new List<Variant>
            {
                new Variant
                {
                    VariantId = Guid.NewGuid().ToString(),
                    AssetQualifier = "AssetQ",
                    VariantDescription = "Desc",
                    DataTypeIds = new List<DataTypeId> { Policies.Current.DataTypes.CreateId("CustomerContent") }
                }
            };
    }
}

    public class DeleteCustomization : CommandCustomization<DeleteCommand, AutoMoqDeleteCommandAttribute>
    {
        public DeleteCustomization(AutoMoqDeleteCommandAttribute attribute) : base(attribute)
        {
        }

        protected override IPostprocessComposer<DeleteCommand> OnPreCustomize(ICustomizationComposer<DeleteCommand> composer)
        {
            return base.OnPreCustomize(composer)
                       .Without(c => c.DataTypePredicate)
                       .Without(c => c.PrivacyDataType);
        }

        protected override void OnPostCustomize(DeleteCommand command)
        {
            base.OnPostCustomize(command);

            var dataType = Policies.Current.DataTypes.CreateId(this.Attribute.DataTypeId);
            command.PrivacyDataType = dataType;

            Type predicateType = this.GetPredicateType(dataType);
            if (predicateType != null)
            {
                command.DataTypePredicate = (IPrivacyPredicate)this.Attribute.Fixture.Create(predicateType);
            }
        }

        private Type GetPredicateType(DataTypeId dataTypeId)
        {
            if (this.Attribute.IncludeDataTypePredicate)
            {
                var ids = Policies.Current.DataTypes.Ids;
                if (dataTypeId == ids.BrowsingHistory)
                {
                    return typeof(BrowsingHistoryPredicate);
                }

                if (dataTypeId == ids.ContentConsumption)
                {
                    return typeof(ContentConsumptionPredicate);
                }

                if (dataTypeId == ids.InkingTypingAndSpeechUtterance)
                {
                    return typeof(InkingTypingAndSpeechUtterancePredicate);
                }

                if (dataTypeId == ids.ProductAndServiceUsage)
                {
                    return typeof(ProductAndServiceUsagePredicate);
                }

                if (dataTypeId == ids.SearchRequestsAndQuery)
                {
                    return typeof(SearchRequestsAndQueryPredicate);
                }
            }

            return null;
        }
    }

    public class ExportCustomization : CommandCustomization<ExportCommand, AutoMoqExportCommandAttribute>
    {
        public ExportCustomization(AutoMoqExportCommandAttribute attribute) : base(attribute)
        {
        }

        protected override IPostprocessComposer<ExportCommand> OnPreCustomize(ICustomizationComposer<ExportCommand> composer)
        {
            return base.OnPreCustomize(composer)
                .Without(c => c.PrivacyDataTypes)
                .Without(x => x.AzureBlobContainerTargetUri);
        }

        protected override void OnPostCustomize(ExportCommand command)
        {
            base.OnPostCustomize(command);

            // For test export commands define fixed PUID/OID for subjects.
            // This is because export enables flighting based on subject and require
            // a known subject in order to process the request successfully.
            // Once flighting is removed this code will not be needed.
            if (command.Subject is MsaSubject)
            {
                var subject = command.Subject as MsaSubject;
                subject.Puid = 0;
            }
            else if (command.Subject is AadSubject)
            {
                var subject = command.Subject as AadSubject;
                subject.ObjectId = Guid.Empty;
            }

            command.PrivacyDataTypes = this.Attribute.DataTypeIds.Select(Policies.Current.DataTypes.CreateId).ToArray();
            command.AzureBlobContainerTargetUri = new Uri("https://www.microsoft.com");
        }
    }

    public class AccountCloseCustomization : CommandCustomization<AccountCloseCommand, AutoMoqAccountCloseCommandAttribute>
    {
        public AccountCloseCustomization(AutoMoqAccountCloseCommandAttribute attribute) : base(attribute)
        {
        }

        protected override void OnPostCustomize(AccountCloseCommand command)
        {
            base.OnPostCustomize(command);

            if (command.Subject is AadSubject2)
            {
                var subject = command.Subject as AadSubject2;
                subject.ObjectId = Guid.Empty;
                subject.TenantIdType = this.Attribute.TenantIdType;
                if (this.Attribute.TenantIdType == TenantIdType.Home)
                {
                    subject.HomeTenantId = subject.TenantId;
                }
            }
        }
    }

    public class AgeOutCustomization : CommandCustomization<AgeOutCommand, AutoMoqAgeOutCommandAttribute>
    {
        public AgeOutCustomization(AutoMoqAgeOutCommandAttribute attribute) : base(attribute)
        {
        }
    }
}
