// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     This class is responsible for converting from agent export storage paths into the final zip file path.
    ///     It is stateful during an aggregation since we need to map agent/assetgroup pairs to directories like 001 and 002 per
    ///     product folder.
    /// </summary>
    public class ExportPathTransformer
    {
        private readonly Func<int, string> productIdToFolderFunc;

        private readonly Dictionary<string, List<Tuple<AgentId, AssetGroupId>>> productToAgentsMap = new Dictionary<string, List<Tuple<AgentId, AssetGroupId>>>();

        /// <summary>
        ///     Create a new ExportPathTransformer. Must be provided a productId to folder mapping function.
        /// </summary>
        /// <param name="productIdToFolderFunc">Function that maps a productId to a folder, or null if it is an unknown productId</param>
        public ExportPathTransformer(Func<int, string> productIdToFolderFunc)
        {
            this.productIdToFolderFunc = productIdToFolderFunc;
        }

        /// <summary>
        ///     Enumerate all the paths that have been produced by this transformer.
        /// </summary>
        public IEnumerable<ExportPathTransformerEntry> EnumeratePaths()
        {
            foreach (KeyValuePair<string, List<Tuple<AgentId, AssetGroupId>>> kvp in this.productToAgentsMap)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    yield return new ExportPathTransformerEntry(
                        kvp.Key + "/" + (i + 1).ToString("000"),
                        kvp.Value[i].Item1,
                        kvp.Value[i].Item2);
                }
            }
        }

        /// <summary>
        ///     Converts a path in the agent staging location to the final zipfile location.
        /// </summary>
        /// <remarks>
        ///     <code>
        ///         Agents are expected to write:
        ///           +-- [productId1]
        ///           | +-- Browse.json
        ///           | +-- Junk.bin
        ///           | +-- ..etc
        ///           +-- [productId2]
        ///           | +-- Location.json
        ///           | +-- ...etc
        ///           +-- [more product Ids]
        ///         
        ///         We will convert from product ids into paths like:
        ///           productId1 == Office365/Excel
        ///           productId2 == Office365/Word
        ///           productId3 == Azure/Storage
        ///         
        ///         If there are two agents, both output data for excel, and one outputs data for word, each agent gets it's own
        ///         folder in the output in the following structure:
        ///           +-- Office
        ///           | +-- Excel
        ///           | | +-- 001
        ///           | | | +-- ProductUsage.json
        ///           | | +-- 002
        ///           | | | +-- ProductUsage.json
        ///           | +-- Word
        ///           | | +-- 001
        ///           | | | +-- ProductUsage.json
        ///     </code>
        /// </remarks>
        /// <param name="path">Path in the agent's staging location</param>
        /// <param name="agentId">The agent's id</param>
        /// <param name="assetGroupId">The agent's asset group id</param>
        /// <returns>The path within the zip file</returns>
        public string TransformPath(string path, AgentId agentId, AssetGroupId assetGroupId)
        {
            // First, get the product path
            List<string> pathParts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            string productPath;
            if (pathParts.Count <= 1 || !int.TryParse(pathParts[0], out int productId) || (productPath = this.productIdToFolderFunc(productId)) == null)
            {
                // When we can't figure it out, dump it in the misc folder.
                productPath = this.productIdToFolderFunc(0);
            }
            else
            {
                // Cut the first part out of the path (the product id, since we turned it into a product path)
                pathParts.RemoveAt(0);
            }

            // Second, get the agent path
            string agentPath = this.GetAgentNumber(productPath, agentId, assetGroupId).ToString("000");

            // Get the rest of the path from the agent:
            string remainingPath = string.Join("/", pathParts);

            // Put it all together.
            return
                productPath + "/" + // Office/Excel/
                agentPath + "/" + //                001/
                remainingPath; //                       foo/bar/baz.bin
        }

        // Each agent has a unique number per product. Agent X may be 002 in folder Office/Excel but 001 in Office/Word because some other agent
        // output excel data first, and no other agent outputs word data.
        private int GetAgentNumber(string path, AgentId agentId, AssetGroupId assetGroupId)
        {
            if (!this.productToAgentsMap.TryGetValue(path, out List<Tuple<AgentId, AssetGroupId>> agentList))
            {
                agentList = new List<Tuple<AgentId, AssetGroupId>>();
                this.productToAgentsMap.Add(path, agentList);
            }

            Tuple<AgentId, AssetGroupId> thisAgent = Tuple.Create(agentId, assetGroupId);

            int idx = agentList.IndexOf(thisAgent);
            if (idx >= 0)
            {
                return idx + 1;
            }

            agentList.Add(Tuple.Create(agentId, assetGroupId));
            return agentList.Count;
        }
    }

    /// <summary>
    ///     A path entry in the <see cref="ExportPathTransformer" />
    /// </summary>
    public class ExportPathTransformerEntry
    {
        /// <summary>
        ///     The agent id
        /// </summary>
        public AgentId AgentId { get; }

        /// <summary>
        ///     The asset group id
        /// </summary>
        public AssetGroupId AssetGroupId { get; }

        /// <summary>
        ///     The path in the zip file
        /// </summary>
        public string Path { get; }

        public ExportPathTransformerEntry(string path, AgentId agentId, AssetGroupId assetGroupId)
        {
            this.Path = path;
            this.AgentId = agentId;
            this.AssetGroupId = assetGroupId;
        }
    }
}
