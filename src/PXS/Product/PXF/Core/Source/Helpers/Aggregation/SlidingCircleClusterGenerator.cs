// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;

    /// <summary>
    /// Clusters nodes using a sliding circle sorting algorthm
    /// </summary>
    public class SlidingCircleClusterGenerator
    {
        /// <summary>
        /// Matching distance used when comparing nodes
        /// </summary>
        private readonly double matchDistance;

        /// <summary>
        /// Distance from center of last node added
        /// </summary>
        private double lastDistanceFromCenter;

        /// <summary>
        /// Clusters that are still within potetential matching range
        /// </summary>
        private List<Cluster<LocationHistoryV1>> circleWindow = new List<Cluster<LocationHistoryV1>>();

        /// <summary>
        /// Clusters that are no longer in matching range and are completely full of all matching nodes
        /// </summary>
        private List<Cluster<LocationHistoryV1>> completedClusters = new List<Cluster<LocationHistoryV1>>();

        /// <summary>
        /// Creates an instance of the SlidingCircleClusterGenerator class
        /// </summary>
        /// <param name="matchDistance">Distance between nodes that are considered a match</param>
        public SlidingCircleClusterGenerator(double matchDistance)
        {
            this.matchDistance = matchDistance;
        }

        /// <summary>
        /// Add a node to the system and attempt to match it with other nodes into a cluster
        /// </summary>
        /// <param name="node">Node to add</param>
        /// <remarks>Nodes must be added in ascending order of distance to the center point</remarks>
        public void AddNode(ClusterNode<LocationHistoryV1> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (node.DistanceFromCenter < this.lastDistanceFromCenter)
            {
                throw new ArgumentException("Out of order node passed to AddNode. Nodes must be added in order of increasing distance from center point");
            }

            // Trim the circle window to only include the nodes within match distance of the new node
            this.lastDistanceFromCenter = node.DistanceFromCenter;
            this.TrimCircleWindow(node.DistanceFromCenter);

            // Find all clusters that match with the new node
            var matchingClusters = this.circleWindow
                .Where(c => c.MatchesAnyChildNodes(node.Latitude, node.Longitude, this.matchDistance, node.PartnerId, node.DeviceId))
                .ToList();

            // If there are no matching clusters, create a new one
            if (matchingClusters.Count == 0)
            {
                var newCluster = new Cluster<LocationHistoryV1>();
                newCluster.AddChildNode(node);
                this.circleWindow.Add(newCluster);
            }
            else
            {
                // Merge all clusters together into the first cluster on the list
                var firstCluster = matchingClusters.First();
                var otherClusters = matchingClusters.Skip(1).ToList();
                otherClusters.ForEach(firstCluster.MergeCluster);

                // Remove the other merged clusters from the circleWindow and then add the new node
                this.circleWindow = this.circleWindow.Except(otherClusters).ToList();

                // Add the new node to the merged cluster
                firstCluster.AddChildNode(node);
            }
        }

        /// <summary>
        /// Called after the last node has been added
        /// </summary>
        public void Complete()
        {
            this.completedClusters.AddRange(this.circleWindow);
            this.circleWindow.Clear();
        }

        /// <summary>
        /// Returns all completed clustered nodes
        /// </summary>
        /// <returns>List of completed clustered nodes</returns>
        public List<LocationHistoryV1> GetCompletedClusteredNodes()
        {
            List<LocationHistoryV1> completedClusteredNodes = new List<LocationHistoryV1>();

            foreach (var cluster in this.completedClusters)
            {
                // The root node will be the primary location point at this location aggregate
                var node = cluster.Root;
                var ids = cluster.ChildNodes.SelectMany(c => c.Properties.Ids).ToList();
                node.Ids = ids;

                if (ids.Count > 1)
                {
                    node.IsAggregate = true;

                    // Child nodes are maintained as a property belonging to the root node of this location aggregate
                    List<LocationHistoryV1> aggregateHistory = cluster.ChildNodes.Select(c => c.Properties).OrderByDescending(c => c.DateTime).ToList();

                    // Since there are more than 1 ids, the category cannot be assumed to be the same as the root.
                    // There is a concept of a 'mixed' category if more than 1 category is present.
                    // Mixed category is intended to be used by the UX layer to show a different icon.
                    node.Category = CalculateClusteredCategory(aggregateHistory);
                    List<LocationV1> nodeAggregateHistory = new List<LocationV1>();

                    // make a copy of first item so it can be modified, otherwise modifying it also modified the node root
                    var nodeCopy = new LocationV1(aggregateHistory.First());
                    nodeCopy.Ids = new List<string> { nodeCopy.Ids.First() };
                    nodeCopy.IsAggregate = false;
                    nodeAggregateHistory.Add(nodeCopy);

                    // begin after first element in the collection
                    for (int i = 1; i < aggregateHistory.Count; i++)
                    {
                        nodeAggregateHistory.Add(aggregateHistory[i]);
                    }

                    node.AggregateHistory = nodeAggregateHistory;
                }
                completedClusteredNodes.Add(node);
            }

            return completedClusteredNodes;
        }

        private static LocationCategory CalculateClusteredCategory(IList<LocationHistoryV1> aggregateHistory)
        {
            if (aggregateHistory.Select(c => c.Category).Distinct().Skip(1).Any())
            {
                return LocationCategory.Mixed;
            }

            return aggregateHistory.First().Category;
        }

        /// <summary>
        /// Trims completed (out of range) clusters out of the circle window
        /// </summary>
        /// <param name="distanceFromCenter">Distance from center of the newest node added</param>
        private void TrimCircleWindow(double distanceFromCenter)
        {
            double minDistance = distanceFromCenter - this.matchDistance;

            var outOfRangeClusters = this.circleWindow.Where(c => c.DistanceFromCenter < minDistance).ToList();

            // Add out of range clusters from circleWindow to the completed list
            this.completedClusters.AddRange(outOfRangeClusters);

            // Update circleWindow to only those above the minimum distance
            this.circleWindow = this.circleWindow.Except(outOfRangeClusters).ToList();
        }
    }
}