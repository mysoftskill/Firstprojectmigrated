// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;

    /// <summary>
    /// Represents a point in 2-D space that can potentially cluster to other points nearby
    /// </summary>
    /// <typeparam name="TNode">Type that represents the properties of this node</typeparam>
    public class ClusterNode<TNode> where TNode : ResourceV1
    {
        /// <summary>
        /// Creates a new node instance
        /// </summary>
        /// <param name="latitude">Latitude of the node</param>
        /// <param name="longitude">Longitude of the node</param>
        /// <param name="distanceFromCenter">Distance from the center</param>
        /// <param name="properties">Properties of this node</param>
        /// <param name="partnerId">The partner identifier of the node.</param>
        /// <param name="deviceId">The device identifier of the node.</param>
        public ClusterNode(double latitude, double longitude, double distanceFromCenter, TNode properties, string partnerId, string deviceId)
        {
            this.Latitude = latitude;
            this.Longitude = longitude;
            this.DistanceFromCenter = distanceFromCenter;
            this.Properties = properties;
            this.PartnerId = partnerId;
            this.DeviceId = deviceId;
        }

        /// <summary>
        /// Latitude of this node
        /// </summary>
        public double Latitude { get; private set; }

        /// <summary>
        /// Longitude of this node
        /// </summary>
        public double Longitude { get; private set; }

        /// <summary>
        /// Distance from center point
        /// </summary>
        public double DistanceFromCenter { get; private set; }

        /// <summary>
        /// Gets the partner identifier for this node.
        /// </summary>
        public string PartnerId { get; private set; }

        /// <summary>
        /// Gets the device identifier.
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Properties of this node
        /// </summary>
        public TNode Properties { get; private set; }
    }

    /// <summary>
    /// A cluster of nodes within matching distance of each other
    /// </summary>
    public class Cluster<TNode> where TNode : ResourceV1
    {
        private readonly List<ClusterNode<TNode>> childNodes = new List<ClusterNode<TNode>>();

        /// <summary>
        /// Gets or sets the root node.
        /// </summary>
        public TNode Root { get; protected set; }

        /// <summary>
        /// Latitude of the cluster center point
        /// </summary>
        public double Latitude { get; protected set; }

        /// <summary>
        /// Longitude of the cluster center point
        /// </summary>
        public double Longitude { get; protected set; }

        /// <summary>
        /// Distance of the cluster from the center point
        /// </summary>
        public double DistanceFromCenter { get; protected set; }

        /// <summary>
        /// Child nodes of the cluster
        /// </summary>
        internal List<ClusterNode<TNode>> ChildNodes
        {
            get { return this.childNodes; }
        }

        /// <summary>
        /// Check if the given point is within the match distance of any of the child nodes
        /// </summary>
        /// <param name="latitude">Latitude to check</param>
        /// <param name="longitude">Longitude to check</param>
        /// <param name="matchDistance">Match distance</param>
        /// <param name="partnerId">The partner identifier.</param>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns>True if the point matches any child node locations</returns>
        public bool MatchesAnyChildNodes(double latitude, double longitude, double matchDistance, string partnerId, string deviceId)
        {
            return this.ChildNodes.Any(n => IsNearbySamePartnerIdAndDeviceId(latitude, longitude, n.Latitude, n.Longitude, matchDistance, partnerId, n.PartnerId, deviceId, n.DeviceId));
        }

        /// <summary>
        /// Adds a child node to this cluster
        /// </summary>
        /// <param name="node">Node to add</param>
        public void AddChildNode(ClusterNode<TNode> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            this.childNodes.Add(node);

            // Update the cluster location and time to the most recently occuring node
            if (this.childNodes.Count == 1 || node.Properties.DateTime > this.Root.DateTime)
            {
                this.Latitude = node.Latitude;
                this.Longitude = node.Longitude;
                this.Root = node.Properties;
            }

            this.DistanceFromCenter = node.DistanceFromCenter;
        }

        /// <summary>
        /// Merges this cluster with a different cluster
        /// </summary>
        /// <param name="cluster">Other cluster to merge with</param>
        public void MergeCluster(Cluster<TNode> cluster)
        {
            if (cluster == null)
            {
                throw new ArgumentNullException("cluster");
            }

            cluster.ChildNodes.ForEach(this.AddChildNode);
        }

        /// <summary>
        /// Checks if 2 points are within specified distance of on another
        /// </summary>
        /// <param name="lat1">Latitude of point 1</param>
        /// <param name="lon1">Longitude of point 1</param>
        /// <param name="lat2">Latitude of point 2</param>
        /// <param name="lon2">Longitude of point 2</param>
        /// <param name="matchDistance">Match distance</param>
        /// <returns>True if the 2 points are within match distance of one another</returns>
        private static bool IsNearby(double lat1, double lon1, double lat2, double lon2, double matchDistance)
        {
            return GeographyHelper.CalculateDistance(lat1, lon1, lat2, lon2) <= matchDistance;
        }

        private static bool IsNearbySamePartnerId(double lat1, double lon1, double lat2, double lon2, double matchDistance, string partnerId1, string partnerId2)
        {
            return IsNearby(lat1, lon1, lat2, lon2, matchDistance) && string.Equals(partnerId1, partnerId2);
        }

        private static bool IsNearbySamePartnerIdAndDeviceId(double lat1, double lon1, double nodeLat, double nodeLon, double matchDistance, string partnerId1, string nodePartnerId, string deviceId1, string nodeDeviceId)
        {
            bool nearbySamePartnerId = IsNearbySamePartnerId(lat1, lon1, nodeLat, nodeLon, matchDistance, partnerId1, nodePartnerId);

            // if there's no device id, then only check if it's nearby the same partner and location
            if (string.IsNullOrWhiteSpace(deviceId1) && string.IsNullOrWhiteSpace(nodeDeviceId))
            {
                return nearbySamePartnerId;
            }

            // but if a device id exists, then check if they match or not
            return nearbySamePartnerId && string.Equals(deviceId1, nodeDeviceId);
        }
    }
}