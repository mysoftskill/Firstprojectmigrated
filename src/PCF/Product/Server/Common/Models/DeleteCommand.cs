namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Represents a delete command.
    /// </summary>
    public class DeleteCommand : PrivacyCommand
    {
        /// <summary>
        /// Maps predicate type to data type, so that we can validate that we got combinations that we expect.
        /// </summary>
        private static readonly IReadOnlyDictionary<Type, DataTypeId> DataTypePredicateTypeMap;

        private TimeRangePredicate timeRangePredicate;
        private DataTypeId dataType;
        private IPrivacyPredicate predicate;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        static DeleteCommand()
        {
            Dictionary<Type, DataTypeId> map = new Dictionary<Type, DataTypeId>
            {
                { typeof(BrowsingHistoryPredicate), Policies.Current.DataTypes.Ids.BrowsingHistory },
                { typeof(ContentConsumptionPredicate), Policies.Current.DataTypes.Ids.ContentConsumption },
                { typeof(DeviceConnectivityAndConfigurationPredicate), Policies.Current.DataTypes.Ids.DeviceConnectivityAndConfiguration },
                { typeof(InkingTypingAndSpeechUtterancePredicate), Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance },
                { typeof(ProductAndServicePerformancePredicate), Policies.Current.DataTypes.Ids.ProductAndServicePerformance },
                { typeof(ProductAndServiceUsagePredicate), Policies.Current.DataTypes.Ids.ProductAndServiceUsage },
                { typeof(SearchRequestsAndQueryPredicate), Policies.Current.DataTypes.Ids.SearchRequestsAndQuery },
                { typeof(SoftwareSetupAndInventoryPredicate), Policies.Current.DataTypes.Ids.SoftwareSetupAndInventory },
                { typeof(PreciseUserLocationPredicate), Policies.Current.DataTypes.Ids.PreciseUserLocation },

                // TRP is special; it doesn't correspond to any specific data type.
                { typeof(TimeRangePredicate), null }
            };

            DataTypePredicateTypeMap = map;

            // As a sanity check, use reflection to discover all classes implementing IPrivacyPredicate in the contracts
            // assembly, and make sure we explicitly know about them here. This guards against a case where a new type of predicate
            // is added to the package and we pick it up without knowing.
            var predicateTypes = typeof(BrowsingHistoryPredicate).Assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IPrivacyPredicate)) && !t.IsInterface && !t.IsAbstract);
            foreach (var type in predicateTypes)
            {
                if (!map.ContainsKey(type))
                {
                    throw new InvalidOperationException("PCF was not configured to introspect on predicate type: " + type.FullName);
                }
            }
        }

        public DeleteCommand(
            AgentId agentId,
            string assetGroupQualifier,
            string verifier,
            string verifierV3,
            CommandId commandId,
            RequestBatchId batchId,
            DateTimeOffset nextVisibleTime,
            IPrivacySubject subject,
            string clientCommandState,
            AssetGroupId assetGroupId,
            string correlationVector,
            DateTimeOffset timestamp,
            string cloudInstance,
            string commandSource,
            bool? processorApplicable,
            bool? controllerApplicable,
            IPrivacyPredicate dataTypePredicate,
            TimeRangePredicate timePredicate,
            DataTypeId dataType,
            DateTimeOffset absoluteExpirationTime,
            QueueStorageType queueStorageType) : base(
                agentId, 
                assetGroupQualifier, 
                verifier,
                verifierV3,
                commandId, 
                batchId, 
                nextVisibleTime, 
                subject, 
                clientCommandState, 
                assetGroupId, 
                correlationVector, 
                timestamp, 
                cloudInstance, 
                commandSource,
                processorApplicable, 
                controllerApplicable,
                absoluteExpirationTime,
                queueStorageType)
        {
            this.DataType = dataType;
            this.Predicate = dataTypePredicate;
            this.TimeRangePredicate = timePredicate;
            this.DataTypeIds = new HashSet<DataTypeId>() { this.DataType };
        }

        /// <summary>
        /// The data type of this delete.
        /// </summary>
        public DataTypeId DataType
        {
            get
            {
                return this.dataType;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                ValidateDataTypePredicateCombination(this.predicate, value);
                this.dataType = value;
            }
        }

        /// <summary>
        /// The data-type specific predicate.
        /// </summary>
        public IPrivacyPredicate Predicate
        {
            get
            {
                return this.predicate;
            }

            set
            {
                ValidateDataTypePredicateCombination(value, this.dataType);
                this.predicate = value;
            }
        }

        /// <summary>
        /// The time range predicate.
        /// </summary>
        public TimeRangePredicate TimeRangePredicate
        {
            get
            {
                return this.timeRangePredicate;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException("The time range predicate may not be null", nameof(value));
                }

                this.timeRangePredicate = value;
            }
        }

        /// <summary>
        /// The command type.
        /// </summary>
        public override PrivacyCommandType CommandType => PrivacyCommandType.Delete;

        /// <inheritdoc />
        protected override ValidOperation ValidationOperationType => ValidOperation.Delete;
        
        /// <inheritdoc />
        public override bool AreDataTypesApplicable(IEnumerable<DataTypeId> dataTypeIds)
        {
            return dataTypeIds.Contains(Policies.Current.DataTypes.Ids.Any)
                || dataTypeIds.Any(dt => this.DataTypeIds.Contains(dt));
        }

        private static void ValidateDataTypePredicateCombination(IPrivacyPredicate predicate, DataTypeId dataTypeId)
        {
            if (predicate != null && DataTypePredicateTypeMap[predicate.GetType()] != dataTypeId)
            {
                throw new ArgumentException($"The provided predicate type: '{predicate?.GetType().Name}' was not compatible with data type: '{dataTypeId}'");
            }
        }
    }
}
