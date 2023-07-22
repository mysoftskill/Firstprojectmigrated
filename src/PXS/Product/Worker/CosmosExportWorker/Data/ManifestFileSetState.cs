// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Azure.Cosmos.Table;

    using Newtonsoft.Json;

    /// <summary>
    ///     state for processing a manifest
    /// </summary>
    /// <remarks>
    ///     Partition key contains the AgentId
    ///     Row key contains the data manifest file path
    /// </remarks>
    public class ManifestFileSetState : 
        TableEntity,
        ITableEntityInitializer,
        ITableEntityStorageExtractor
    {
        // DO NOT change the numeric values of these enum values
        private enum FileTagMode
        {
            JsonSerialized = 0,
            NameOnlyNewlineList = 1,
        }

        private const string PropDataManifestCreate = "DataManifestCreate";
        private const string PropReqManifestCreate = "RequestManifestCreate";
        private const string PropDataFilePathsJson = "DataFilePathsJson";
        private const string PropDataFileNameLines = "DataFileNameLines";
        private const string PropDataManifestHash = "DataManifestHash";
        private const string PropReqManifestHash = "RequestManifestHash";
        private const string PropReqManifestPath = "RequestManifestPath";
        private const string PropFileTagMode = "FileTagMode";
        private const string PropCosmosTag = "CosmosTag";
        private const string PropCounter = "Counter";

        /// <summary>
        ///     Gets or sets request file
        /// </summary>
        public string RequestManifestPath { get; set; }

        /// <summary>
        ///     Gets or sets request file
        /// </summary>
        public int Counter { get; set; }

        /// <summary>
        ///     Gets or sets the agent id
        /// </summary>
        [IgnoreProperty]
        public string AgentId
        {
            get => TableUtilities.UnescapeKey(this.PartitionKey);
            set => this.PartitionKey = TableUtilities.EscapeKey(value);
        }

        /// <summary>
        ///     Gets or sets the manifest path
        /// </summary>
        [IgnoreProperty]
        public string ManifestPath
        {
            get => TableUtilities.UnescapeKey(this.RowKey);
            set => this.RowKey = TableUtilities.EscapeKey(value);
        }

        /// <summary>
        ///     Gets or sets the names of the files in the file set
        /// </summary>
        [IgnoreProperty]
        public ICollection<string> DataFileTags { get; set; }

        /// <summary>
        ///     Gets or sets the time the data file manifest was created
        /// </summary>
        [IgnoreProperty]
        public DateTimeOffset? DataFileManifestCreateTime { get; set; }

        /// <summary>
        ///     Gets or sets the time the request manifest was created
        /// </summary>
        [IgnoreProperty]
        public DateTimeOffset? RequestManifestCreateTime { get; set; }

        /// <summary>
        ///     Gets or sets an order independent hash of the list of files in the data file manifest
        /// </summary>
        [IgnoreProperty]
        public int? DataFileManifestHash { get; set; }

        /// <summary>
        ///     Gets or sets an order independent hash of the list of commands in the request manifest
        /// </summary>
        [IgnoreProperty]
        public int? RequestManifestHash { get; set; }

        /// <summary>
        ///     Gets or sets the time the request manifest was created
        /// </summary>
        [IgnoreProperty]
        public DateTimeOffset? FirstProcessingTime { get; set; }

        /// <summary>
        ///     Initializes the class with the raw table object
        /// </summary>
        /// <param name="rawTableObject">raw table object</param>
        public void Initialize(object rawTableObject)
        {
            DynamicTableEntity entity = rawTableObject as DynamicTableEntity;
            int fileTagMode;

            if (entity == null)
            {
                throw new ArgumentException(
                    "rawTableObject was expected to be of type {0} but was instead found to be of type {1}"
                        .FormatInvariant(
                            typeof(DynamicTableEntity).Name,
                            rawTableObject.GetType().FullName));
            }

            this.PartitionKey = entity.PartitionKey;
            this.Timestamp = entity.Timestamp;
            this.RowKey = entity.RowKey;
            this.ETag = entity.ETag;

            this.DataFileManifestCreateTime = entity.GetDateTimeOffset(ManifestFileSetState.PropDataManifestCreate);
            this.RequestManifestCreateTime = entity.GetDateTimeOffset(ManifestFileSetState.PropReqManifestCreate);

            this.DataFileManifestHash = entity.GetInt(ManifestFileSetState.PropDataManifestHash);
            this.RequestManifestHash = entity.GetInt(ManifestFileSetState.PropReqManifestHash);

            this.RequestManifestPath = entity.GetString(ManifestFileSetState.PropReqManifestPath) ?? string.Empty;
            this.Counter = entity.GetInt(ManifestFileSetState.PropCounter) ?? 0;

            fileTagMode = entity.GetInt(ManifestFileSetState.PropFileTagMode) ?? (int)FileTagMode.JsonSerialized;

            if (fileTagMode == (int)FileTagMode.JsonSerialized)
            {
                string json = entity.GetString(ManifestFileSetState.PropDataFilePathsJson);

                this.DataFileTags = json != null ?
                    JsonConvert.DeserializeObject<ICollection<string>>(json) :
                    new List<string>();
            }
            else if (fileTagMode == (int)FileTagMode.NameOnlyNewlineList)
            {
                string[] fileNames;
                string tagLines = entity.GetString(ManifestFileSetState.PropDataFileNameLines);

                fileNames = tagLines?.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (fileNames?.Length > 0)
                {
                    string cosmosTag = entity.GetString(ManifestFileSetState.PropCosmosTag) ?? string.Empty;
                    string agentId = this.AgentId;

                    this.DataFileTags = fileNames.Select(n => cosmosTag + "." + agentId + "." + n).ToList();
                }
                else
                {
                    this.DataFileTags = new List<string>();
                }
            }
            else
            {
                throw new ArgumentException("Unexpected file tag mode of " + fileTagMode.ToStringInvariant() + " found");
            }
        }

        /// <summary>
        ///     Geneate a representation that will be sent to storage
        /// </summary>
        /// <returns>object instance that should be sent to storage</returns>
        /// <remarks>
        ///     the representation generated can be more dynamic than the properties directly exposed by the object implementing
        ///      this interface, which allows the object to vary the storage columns based on the object state, such as breaking
        ///      a single column into multiple for storage purposes to work around column size limitations
        /// </remarks>
        public ITableEntity ExtractStorageRepresentation()
        {
            IDictionary<string, EntityProperty> props = new Dictionary<string, EntityProperty>(StringComparer.OrdinalIgnoreCase);
            DynamicTableEntity result;
            string dataFileNames = string.Empty;
            string cosmosTag = string.Empty;

            result = new DynamicTableEntity(this.PartitionKey, this.RowKey, this.ETag, props)
            {
                [ManifestFileSetState.PropDataManifestCreate] = 
                    EntityProperty.GeneratePropertyForDateTimeOffset(this.DataFileManifestCreateTime),

                [ManifestFileSetState.PropReqManifestCreate] = 
                    EntityProperty.GeneratePropertyForDateTimeOffset(this.RequestManifestCreateTime),

                [ManifestFileSetState.PropReqManifestPath] = EntityProperty.GeneratePropertyForString(this.RequestManifestPath),
                [ManifestFileSetState.PropFileTagMode] = EntityProperty.GeneratePropertyForInt((int)FileTagMode.NameOnlyNewlineList),
                [ManifestFileSetState.PropCounter] = EntityProperty.GeneratePropertyForInt(this.Counter),

                [ManifestFileSetState.PropDataManifestHash] = EntityProperty.GeneratePropertyForInt(this.DataFileManifestHash),
                [ManifestFileSetState.PropReqManifestHash] = EntityProperty.GeneratePropertyForInt(this.RequestManifestHash),

                Timestamp = this.Timestamp,
            };

            if (this.DataFileTags?.Count > 0)
            {
                // all tags are expected to come from the same cosmos path, so all files should have the same prefix Cosmos tag
                cosmosTag = Utility.SplitFileTag(this.DataFileTags.First()).CosmosTag;
                dataFileNames = string.Join("\n", this.DataFileTags.Select(t => Utility.SplitFileTag(t).Name));
            }

            result[ManifestFileSetState.PropDataFileNameLines] = EntityProperty.GeneratePropertyForString(dataFileNames);
            result[ManifestFileSetState.PropCosmosTag] = EntityProperty.GeneratePropertyForString(cosmosTag);

            // preserve the old property but hardcode it to be an empty array
            result[ManifestFileSetState.PropDataFilePathsJson] = EntityProperty.GeneratePropertyForString("[]");

            return result;
        }
    }
}
