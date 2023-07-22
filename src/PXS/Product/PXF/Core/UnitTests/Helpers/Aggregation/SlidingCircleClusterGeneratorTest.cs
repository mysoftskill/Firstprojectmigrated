// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers
{
    using System.Collections.Generic;
    using System.Data.Spatial;
    using System.Linq;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// SlidingCircleClusterGenerator Test
    /// </summary>
    [TestClass]
    public class SlidingCircleClusterGeneratorTest
    {
        private const double DefaultLatDelta = 0.000199;

        private const double DefaultLonDelta = 0.00027;

        private const double DefaultMatchDistanceMeters = 125;

        /// <summary>
        /// Tests a single cluster on the map consisting of 4 nodes
        /// </summary>
        [TestMethod]
        public void SingleCluster()
        {
            ////                  123456789012345678901234567890
            const string Nodes = ".............................." + // 1
                                 ".........AB..................." + // 2
                                 ".........CD..................." + // 3
                                 ".............................." + // 4
                                 ".............................." + // 5
                                 ".............................." + // 6
                                 ".............................." + // 7
                                 ".............................." + // 8
                                 ".............................." + // 9
                                 "..............................";  // 10
            Dictionary<string, Node> nodeMap = this.ConvertTestLayoutToNodeMap(Nodes, 30, DefaultLatDelta, DefaultLonDelta);
            var clusters = this.ClusterNodes(nodeMap, DefaultMatchDistanceMeters);

            // Verifications
            Assert.AreEqual(1, clusters.Count);
            ////Assert.IsTrue(clusters[0].DistanceFromAnchor > 0);
            Assert.AreEqual("A,C,B,D", string.Join(",", clusters[0].Ids));
        }

        /// <summary>
        /// Test a single cluster with a tiny match distance that results 4 distance output clusters
        /// </summary>
        [TestMethod]
        public void SingleClusterWithTinyMatchDistance()
        {
            ////                  123456789012345678901234567890
            const string Nodes = ".............................." + // 1
                                 ".........AB..................." + // 2
                                 ".........CD..................." + // 3
                                 ".............................." + // 4
                                 ".............................." + // 5
                                 ".............................." + // 6
                                 ".............................." + // 7
                                 ".............................." + // 8
                                 ".............................." + // 9
                                 "..............................";  // 10
            var nodeMap = this.ConvertTestLayoutToNodeMap(Nodes, 30, DefaultLatDelta, DefaultLonDelta);
            var clusters = this.ClusterNodes(nodeMap, 1);

            // Verifications
            Assert.AreEqual(4, clusters.Count);
        }

        /// <summary>
        /// Test 2 clusters
        /// </summary>
        [TestMethod]
        public void TwoClusters()
        {
            ////                  123456789012345678901234567890
            const string Nodes = ".............................." + // 1
                                 ".........AB..................." + // 2
                                 ".............................." + // 3
                                 ".............................." + // 4
                                 ".............................." + // 5
                                 ".....................C........" + // 6
                                 "......................D......." + // 7
                                 ".............................." + // 8
                                 ".............................." + // 9
                                 "..............................";  // 10
            var nodeMap = this.ConvertTestLayoutToNodeMap(Nodes, 30, DefaultLatDelta, DefaultLonDelta);
            var clusters = this.ClusterNodes(nodeMap, DefaultMatchDistanceMeters);

            // Verifications
            Assert.AreEqual(2, clusters.Count);
            Assert.AreEqual("A,B", string.Join(",", clusters[0].Ids));
            Assert.AreEqual("C,D", string.Join(",", clusters[1].Ids));
        }

        /// <summary>
        /// Test 3 clusters
        /// </summary>
        [TestMethod]
        public void ThreeClusters()
        {
            ////                  123456789012345678901234567890
            const string Nodes = ".............................." + // 1
                                 "..AB.........................." + // 2
                                 "....C........................." + // 3
                                 ".............................." + // 4
                                 "........DE...................." + // 5
                                 ".............................." + // 6
                                 ".............................." + // 7
                                 ".............................." + // 8
                                 ".........................H...." + // 9
                                 ".......................FG.....";  // 10
            var nodeMap = this.ConvertTestLayoutToNodeMap(Nodes, 30, DefaultLatDelta, DefaultLonDelta);
            var clusters = this.ClusterNodes(nodeMap, DefaultMatchDistanceMeters);

            // Verifications
            Assert.AreEqual(3, clusters.Count);
            Assert.AreEqual("A,B,C", string.Join(",", clusters[0].Ids));
            Assert.AreEqual("D,E", string.Join(",", clusters[1].Ids));
            Assert.AreEqual("F,G,H", string.Join(",", clusters[2].Ids));
        }

        /// <summary>
        /// Test a single cluster arranged in a diagonal 
        /// </summary>
        [TestMethod]
        public void DiagonalCluster()
        {
            ////                  123456789012345678901234567890
            const string Nodes = ".............................." + // 1
                                 ".........A...................." + // 2
                                 "..........B..................." + // 3
                                 "...........C.................." + // 4
                                 "............DE................" + // 5
                                 "..............F..............." + // 6
                                 "...............G.............." + // 7
                                 "................H............." + // 8
                                 ".............................." + // 9
                                 "..............................";  // 10
            var nodeMap = this.ConvertTestLayoutToNodeMap(Nodes, 30, DefaultLatDelta * 3, DefaultLonDelta * 3);
            var clusters = this.ClusterNodes(nodeMap, DefaultMatchDistanceMeters);

            // Verifications
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual("A,B,C,D,E,F,G,H", string.Join(",", clusters[0].Ids));
        }

        /// <summary>
        /// Test a single cluster arranged in a reverse diagonal
        /// </summary>
        [TestMethod]
        public void ReverseDiagonalCluster()
        {
            ////                  123456789012345678901234567890
            const string Nodes = ".............................." + // 1
                                 "............GH................" + // 2
                                 "...........F.................." + // 3
                                 "..........E..................." + // 4
                                 ".........D...................." + // 5
                                 "........C....................." + // 6
                                 ".......B......................" + // 7
                                 "......A......................." + // 8
                                 ".............................." + // 9
                                 "..............................";  // 10
            var nodeMap = this.ConvertTestLayoutToNodeMap(Nodes, 30, DefaultLatDelta * 3, DefaultLonDelta * 3);
            var clusters = this.ClusterNodes(nodeMap, DefaultMatchDistanceMeters);

            // Verifications
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual("A,B,C,D,E,F,G,H", string.Join(",", clusters[0].Ids));
        }

        /// <summary>
        /// Test a single cluster arranged in 'Z' pattern
        /// </summary>
        [TestMethod]
        public void ZeeCluster()
        {
            ////                  123456789012345678901234567890
            const string Nodes = ".............................." + // 1
                                 "...ABCDEFGHIJK................" + // 2
                                 "............L................." + // 3
                                 "...........M.................." + // 4
                                 ".........N...................." + // 5
                                 ".......O......................" + // 6
                                 ".....P........................" + // 7
                                 "....Q........................." + // 8
                                 "...RSTUVWXYZ12................" + // 9
                                 "..............................";  // 10
            var nodeMap = this.ConvertTestLayoutToNodeMap(Nodes, 30, DefaultLatDelta, DefaultLonDelta);
            var clusters = this.ClusterNodes(nodeMap, DefaultMatchDistanceMeters);

            // Verifications
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual("A,B,C,D,Q,R,P,E,S,T,O,F,U,G,V,N,W,H,X,I,M,Y,J,L,Z,K,1,2", string.Join(",", clusters[0].Ids));
        }

        /// <summary>
        /// Test a single cluster arranged in 'V' pattern
        /// </summary>
        [TestMethod]
        public void VCluster()
        {
            ////                  123456789012345678901234567890
            const string Nodes = ".............................." + // 1
                                 "...A.............O............" + // 2
                                 "....B...........N............." + // 3
                                 ".....C.........M.............." + // 4
                                 "......D.......L..............." + // 5
                                 ".......E.....K................" + // 6
                                 "........F...J................." + // 7
                                 ".........G.I.................." + // 8
                                 "..........H..................." + // 9
                                 "..............................";  // 10
            var nodeMap = this.ConvertTestLayoutToNodeMap(Nodes, 30, DefaultLatDelta, DefaultLonDelta);
            var clusters = this.ClusterNodes(nodeMap, DefaultMatchDistanceMeters);

            // Verifications
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual("A,B,C,D,E,F,G,H,I,J,K,L,M,N,O", string.Join(",", clusters[0].Ids));
        }

        /// <summary>
        /// Test a single cluster arranged in 'H' pattern
        /// </summary>
        [TestMethod]
        public void ClusterH()
        {
            ////                  123456789012345678901234567890
            const string Nodes = ".............................." + // 1
                                 "...A........P................." + // 2
                                 "...B........Q................." + // 3
                                 "...C........R................." + // 4
                                 "...DHIJKLMNOS................." + // 5
                                 "...E........T................." + // 6
                                 "...F........U................." + // 7
                                 "...G........V................." + // 8
                                 ".............................." + // 9
                                 "..............................";  // 10
            var nodeMap = this.ConvertTestLayoutToNodeMap(Nodes, 30, DefaultLatDelta * 3, DefaultLonDelta * 3);
            var clusters = this.ClusterNodes(nodeMap, DefaultMatchDistanceMeters);

            // Verifications
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual("A,B,C,D,E,H,F,I,G,J,K,L,M,N,O,P,Q,R,S,T,U,V", string.Join(",", clusters[0].Ids));
        }

        /// <summary>
        /// Test a single cluster arranged in an arc (sort of)
        /// </summary>
        [TestMethod]
        public void ClusterArc()
        {
            ////                  123456789012345678901234567890
            const string Nodes = "................A............." + // 1
                                 "...............B.............." + // 2
                                 ".............DC..............." + // 3
                                 "...........E.................." + // 4
                                 "........GF...................." + // 5
                                 ".....IH......................." + // 6
                                 "...LKJ........................" + // 7
                                 ".NM..........................." + // 8
                                 ".............................." + // 9
                                 "..............................";  // 10
            var nodeMap = this.ConvertTestLayoutToNodeMap(Nodes, 30, DefaultLatDelta, DefaultLonDelta);
            var clusters = this.ClusterNodes(nodeMap, DefaultMatchDistanceMeters);

            // Verifications
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual("N,L,M,K,I,J,H,G,F,E,D,C,B,A", string.Join(",", clusters[0].Ids));
        }

        /// <summary>
        /// Convert the ascii-art layout into a set of nodes with appropriate lat/lon locations
        /// </summary>
        /// <param name="nodeLayout">Ascii-art layout string</param>
        /// <param name="columns">Number of columns in the layout</param>
        /// <param name="latDelta">Latitude delta each row represents</param>
        /// <param name="lonDelta">Longitude delta each column represents</param>
        /// <returns>Dictionary of all nodes in the layout keyed by the char value in the cell</returns>
        private Dictionary<string, Node> ConvertTestLayoutToNodeMap(string nodeLayout, int columns, double latDelta, double lonDelta)
        {
            DbGeography centerPoint = GeographyHelper.CreatePoint(0, 0);

            var nodeMap = new Dictionary<string, Node>();
            for (int i = 0; i < nodeLayout.Length; i++)
            {
                if (nodeLayout[i] != '.')
                {
                    int x = i % columns;
                    int y = i / columns;

                    var location = new LocationHistoryV1
                    {
                        Latitude = centerPoint.Latitude.Value - (y*latDelta),
                        Longitude = centerPoint.Longitude.Value + (x*lonDelta),
                        Ids = new[] { nodeLayout[i].ToString() }
                    };

                    var node = new Node(location);

                    node.Properties = location;
                    nodeMap.Add(node.Properties.Ids.First(), node);
                }
            }

            return nodeMap;
        }

        /// <summary>
        /// Cluster all of the nodes using the SlidingArcClusterGenerator class
        /// </summary>
        /// <param name="nodeMap">Node map</param>
        /// <param name="matchDistance">Matching distance to use</param>
        /// <returns>List of clusters</returns>
        private IList<LocationHistoryV1> ClusterNodes(Dictionary<string, Node> nodeMap, double matchDistance)
        {
            var orderedList = nodeMap.Values.OrderBy(n => n.DistanceFromCenter).ToList();

            var clusterer = new SlidingCircleClusterGenerator(matchDistance);
            foreach (var node in orderedList)
            {
                var clusterNode = new ClusterNode<LocationHistoryV1>(
                    node.Latitude,
                    node.Longitude,
                    node.DistanceFromCenter,
                    node.Properties,
                    node.PartnerId,
                    node.DeviceId);
                clusterer.AddNode(clusterNode);
            }

            clusterer.Complete();
            return clusterer.GetCompletedClusteredNodes();
        }
    }
}