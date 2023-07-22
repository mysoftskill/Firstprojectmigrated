// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests
{
    using System;
    using System.Linq;
    using System.Spatial;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    /// <summary>
    ///     GeographyPointConverter Test
    /// </summary>
    [TestClass]
    public class GeographyPointConverterTest
    {
        [TestMethod]
        public void GeographyPointSerializeTest()
        {
            var locationResourceV2 = new LocationResourceV2
            {
                Location = GeographyPoint.Create(42.0, 43.0)
            };

            string json = JsonConvert.SerializeObject(locationResourceV2);
            LocationResourceV2 roundTrip = JsonConvert.DeserializeObject<LocationResourceV2>(json);

            Assert.IsNotNull(roundTrip.Location);
            Assert.AreEqual(locationResourceV2.Location.Latitude, roundTrip.Location.Latitude);
            Assert.AreEqual(locationResourceV2.Location.Longitude, roundTrip.Location.Longitude);
        }
    }
}
