// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers
{
    using System;
    using System.Linq;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests ClusterNode and Cluster classes
    /// </summary>
    [TestClass]
    public class ClusterTest
    {
        private const string PartnerId = "partner1";
        private const string DeviceId = "My Unique Device Id";

        /// <summary>
        /// Tests the ClusterNode constructor and properties
        /// </summary>
        [TestMethod]
        public void ClusterNodeNodeConstructor()
        {
            var node1 = new ClusterNode<TestResource>(
                45.5,
                -122.123,
                54,
                new TestResource(),
                PartnerId,
                DeviceId);

            Assert.IsNotNull(node1);
            Assert.AreEqual(45.5, node1.Latitude);
            Assert.AreEqual(-122.123, node1.Longitude);
            Assert.AreEqual(54, node1.DistanceFromCenter);
            Assert.IsNotNull(node1.Properties);
            Assert.IsInstanceOfType(node1.Properties, typeof(TestResource));
        }

        /// <summary>
        /// Tests adding a single node to a cluster
        /// </summary>
        [TestMethod]
        public void ClusterWithOneNode()
        {
            var cluster = new Cluster<TestResource>();
            cluster.AddChildNode(new ClusterNode<TestResource>(
                45.5,
                -122.123,
                54,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-01-02"),
                    Ids = new[] { "D3472CCF-5D0C-4696-96E7-5FB42DE5C33D" }
                },
                PartnerId,
                DeviceId));

            Assert.AreEqual(45.5, cluster.Latitude);
            Assert.AreEqual(-122.123, cluster.Longitude);
            Assert.AreEqual(54, cluster.DistanceFromCenter);
            Assert.AreEqual(DateTimeOffset.Parse("2016-01-02"), cluster.Root.DateTime);
            Assert.AreEqual(1, cluster.Root.Ids.Count());
            Assert.AreEqual("D3472CCF-5D0C-4696-96E7-5FB42DE5C33D", cluster.Root.Ids.ElementAt(0));
        }

        /// <summary>
        /// Tests a cluster with 2 nodes
        /// </summary>
        [TestMethod]
        public void ClusterWithTwoNodes()
        {
            var cluster = new Cluster<TestResource>();
            cluster.AddChildNode(new ClusterNode<TestResource>(
                45.5,
                -122.123,
                54,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-01-02"),
                    Ids = new[] { "D3472CCF-5D0C-4696-96E7-5FB42DE5C33D" }
                },
                PartnerId,
                DeviceId));
            cluster.AddChildNode(new ClusterNode<TestResource>(
                45.75,
                -122.256,
                55,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-05-05"),
                    Ids = new[] { "4AB0F7C4-0F54-450E-AF7E-DA9BC6D5929C" }
                },
                PartnerId,
                DeviceId));

            Assert.AreEqual(2, cluster.ChildNodes.Count);
            Assert.AreEqual(DateTimeOffset.Parse("2016-05-05"), cluster.Root.DateTime);
            Assert.AreEqual("4AB0F7C4-0F54-450E-AF7E-DA9BC6D5929C", cluster.Root.Ids.First());
        }

        /// <summary>
        /// Tests a cluster with 3 nodes
        /// </summary>
        [TestMethod]
        public void ClusterWithThreeNodes()
        {
            var cluster = new Cluster<TestResource>();
            cluster.AddChildNode(new ClusterNode<TestResource>(
                45.5,
                -122.123,
                54,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-01-02"),
                    Ids = new[] { "D3472CCF-5D0C-4696-96E7-5FB42DE5C33D" }
                },
                PartnerId,
                DeviceId));
            cluster.AddChildNode(new ClusterNode<TestResource>(
                45.75,
                -122.256,
                55,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-05-05"),
                    Ids = new[] { "4AB0F7C4-0F54-450E-AF7E-DA9BC6D5929C" }
                },
                PartnerId,
                DeviceId));
            cluster.AddChildNode(new ClusterNode<TestResource>(
                45.88,
                -121.990,
                52,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-03-01"),
                    Ids = new[] { "B4438200-F9A5-448C-B7B5-137C86504AED" }
                },
                PartnerId,
                DeviceId));

            Assert.AreEqual(3, cluster.ChildNodes.Count);
            Assert.AreEqual(DateTimeOffset.Parse("2016-05-05"), cluster.Root.DateTime);
            Assert.AreEqual("4AB0F7C4-0F54-450E-AF7E-DA9BC6D5929C", cluster.Root.Ids.First());
        }

        /// <summary>
        /// Tests the MatchesAnyChildNodes method
        /// </summary>
        [TestMethod]
        public void ClusterMatchesAnyChildNodes()
        {
            var cluster = new Cluster<TestResource>();
            cluster.AddChildNode(new ClusterNode<TestResource>(
                45.5,
                -122.123,
                54,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-01-02"),
                    Ids = new[] { "D3472CCF-5D0C-4696-96E7-5FB42DE5C33D" }
                },
                PartnerId,
                DeviceId));
            Assert.IsFalse(cluster.MatchesAnyChildNodes(0, 0, 100, PartnerId, DeviceId));
            Assert.IsTrue(cluster.MatchesAnyChildNodes(45.5, -122.123, 100, PartnerId, DeviceId));

            cluster.AddChildNode(new ClusterNode<TestResource>(
                45.75,
                -122.256,
                55,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-05-05"),
                    Ids = new[] { "4AB0F7C4-0F54-450E-AF7E-DA9BC6D5929C" }
                },
                PartnerId,
                DeviceId));
            Assert.IsFalse(cluster.MatchesAnyChildNodes(0, 0, 100, PartnerId, DeviceId));
            Assert.IsTrue(cluster.MatchesAnyChildNodes(45.5, -122.123, 100, PartnerId, DeviceId));

            // different partners do not group
            Assert.IsFalse(cluster.MatchesAnyChildNodes(45.5, -122.123, 100, PartnerId +"new", DeviceId));

            Assert.IsTrue(cluster.MatchesAnyChildNodes(45.75, -122.256, 100, PartnerId, DeviceId));
            
            // different partners do not group
            Assert.IsFalse(cluster.MatchesAnyChildNodes(45.75, -122.256, 100, PartnerId + "new", DeviceId));

            Assert.IsFalse(cluster.MatchesAnyChildNodes(45.4, -122.123, 100, PartnerId, DeviceId));
        }

        /// <summary>
        /// Tests the MatchesAnyChildNodes method and the behavior associated with device ids
        /// </summary>
        [TestMethod]
        public void ClusterMatchesAnyChildNodesDeviceIdTest()
        {
            // Arrange
            // Create three clusters at differnt points and device ids
            const double Latitude = 45.5;
            const double Longitude = -122.123;
            var cluster = new Cluster<TestResource>();
            cluster.AddChildNode(new ClusterNode<TestResource>(
                Latitude,
                Longitude,
                54,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-01-02"),
                    Ids = new[] { "D3472CCF-5D0C-4696-96E7-5FB42DE5C33D" }
                },
                PartnerId,
                DeviceId));

            // creates cluster with null device id, but same point as previous
            cluster.AddChildNode(new ClusterNode<TestResource>(
                Latitude,
                Longitude,
                55,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-05-05"),
                    Ids = new[] { "4AB0F7C4-0F54-450E-AF7E-DA9BC6D5929C" }
                },
                PartnerId,
                deviceId: null));

            // creates a cluster at a different lat/long
            cluster.AddChildNode(new ClusterNode<TestResource>(
                45.75,
                -122.256,
                55,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-05-05"),
                    Ids = new[] { "4AB0F7C4-0F54-450E-AF7E-DA9BC6D5929C" }
                },
                PartnerId,
                DeviceId));

            // same device id does group
            Assert.IsTrue(cluster.MatchesAnyChildNodes(Latitude, Longitude, 100, PartnerId, DeviceId));

            // different devices do not group
            Assert.IsFalse(cluster.MatchesAnyChildNodes(Latitude, Longitude, 100, PartnerId, DeviceId + "new"));

            // same device id does group
            Assert.IsTrue(cluster.MatchesAnyChildNodes(Latitude, Longitude, 100, PartnerId, DeviceId));
            
            // different devices do not group
            Assert.IsFalse(cluster.MatchesAnyChildNodes(Latitude, Longitude, 100, PartnerId, DeviceId + "new"));

            // null or empty device id matches the cluster with null device id
            Assert.IsTrue(cluster.MatchesAnyChildNodes(Latitude, Longitude, 100, PartnerId, deviceId: string.Empty));
        }

        [TestMethod]
        public void ClusterMatchesAnyChildNodesNullDeviceId()
        {
            // Arrange
            // Create 1 cluster with a known device id
            var cluster = new Cluster<TestResource>();
            cluster.AddChildNode(new ClusterNode<TestResource>(
                45.5,
                -122.123,
                54,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-01-02"),
                    Ids = new[] { "D3472CCF-5D0C-4696-96E7-5FB42DE5C33D" }
                },
                PartnerId,
                DeviceId));

            // same device id does group
            Assert.IsTrue(cluster.MatchesAnyChildNodes(45.5, -122.123, 100, PartnerId, DeviceId));

            // null, empty, or whitespace device id does not match the cluster that has a device id
            Assert.IsFalse(cluster.MatchesAnyChildNodes(45.75, -122.256, 100, PartnerId, deviceId: string.Empty));
            Assert.IsFalse(cluster.MatchesAnyChildNodes(45.75, -122.256, 100, PartnerId, deviceId: null));
            Assert.IsFalse(cluster.MatchesAnyChildNodes(45.75, -122.256, 100, PartnerId, " "));
        }

        [TestMethod]
        public void ClusterMatchesAnyChildNodesNullDeviceIdAlreadyClustered()
        {
            const double Latitude = 45.5;
            const double Longitude = -122.123;

            // Arrange
            // Create 1 cluster with a known device id
            var cluster = new Cluster<TestResource>();
            cluster.AddChildNode(new ClusterNode<TestResource>(
                Latitude,
                Longitude,
                54,
                new TestResource
                {
                    DateTime = DateTimeOffset.Parse("2016-01-02"),
                    Ids = new[] { "D3472CCF-5D0C-4696-96E7-5FB42DE5C33D" }
                },
                PartnerId,
                null));

            // null, empty, or whitespace device id matches the cluster with a null device id
            Assert.IsTrue(cluster.MatchesAnyChildNodes(Latitude, Longitude, 100, PartnerId, deviceId: string.Empty));
            Assert.IsTrue(cluster.MatchesAnyChildNodes(Latitude, Longitude, 100, PartnerId, deviceId: null));
            Assert.IsTrue(cluster.MatchesAnyChildNodes(Latitude, Longitude, 100, PartnerId, " "));
        }

        /// <summary>
        /// Tests merger 2 clusters together
        /// </summary>
        [TestMethod]
        public void ClusterMerge()
        {
            var cluster1 = new Cluster<TestResource>();
            cluster1.AddChildNode(new ClusterNode<TestResource>(45.45, -122.25, 500, new TestResource(), PartnerId, DeviceId));

            var cluster2 = new Cluster<TestResource>();
            cluster2.AddChildNode(new ClusterNode<TestResource>(43, -120, 500, new TestResource(), PartnerId, DeviceId));

            cluster1.MergeCluster(cluster2);

            Assert.AreEqual(2, cluster1.ChildNodes.Count);
        }

        private class TestResource : ResourceV1
        {
        }
    }
}