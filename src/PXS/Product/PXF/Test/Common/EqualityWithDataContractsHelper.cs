// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Privacy.DataContracts.ExportTypes;

    public class EqualityWithDataContractsHelper
    {
        public static void AreEqual(ExportStatusRecord expected, ExportStatusRecord actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }
            Assert.AreEqual(expected.UserId, actual.UserId);
            Assert.AreEqual(expected.ExportId, actual.ExportId);
            Assert.AreEqual(expected.IsComplete, actual.IsComplete);
            Assert.AreEqual(expected.LastError, actual.LastError);
            Assert.AreEqual(expected.LastSessionStart, actual.LastSessionStart);
            Assert.AreEqual(expected.LastSessionEnd, actual.LastSessionEnd);
            Assert.AreEqual(expected.Ticket, actual.Ticket);
            Assert.AreEqual(expected.StartTime, actual.StartTime);
            Assert.AreEqual(expected.EndTime, actual.EndTime);
            if (expected.DataTypes == null)
            {
                Assert.IsNull(actual.DataTypes, "Actual.DataTypes: {0}", actual.DataTypes);
            }
            else
            {
                Assert.AreEqual(expected.DataTypes.Count, actual.DataTypes.Count, "Different count of DataTypes");
                for (int i = 0; i < expected.DataTypes.Count; i++)
                {
                    Assert.AreEqual(expected.DataTypes[i], actual.DataTypes[i]);
                }
            }
            if (expected.Flights == null)
            {
                Assert.IsNull(actual.Flights, "Actual.Flights: {0}", actual.Flights);
            }
            else
            {
                Assert.AreEqual(expected.Flights.Length, actual.Flights.Length, "Different count of Flights");
                for (int i = 0; i < expected.Flights.Length; i++)
                {
                    Assert.AreEqual(expected.Flights[i], actual.Flights[i]);
                }
            }

            if (expected.Resources == null)
            {
                Assert.IsNull(actual.Resources, "Actual.Resources: {0}", actual.Resources);
            }
            else
            {
                Assert.AreEqual(expected.Resources.Count, actual.Resources.Count, "Different count of Categories");
                for (int i = 0; i < expected.Resources.Count; i++)
                {
                    Assert.AreEqual(expected.Resources[i].ResourceDataType, actual.Resources[i].ResourceDataType);
                    Assert.AreEqual(expected.Resources[i].IsComplete, actual.Resources[i].IsComplete);
                    Assert.AreEqual(expected.Resources[i].LastSessionStart, actual.Resources[i].LastSessionStart);
                    Assert.AreEqual(expected.Resources[i].LastSessionEnd, actual.Resources[i].LastSessionEnd);
                }
            }
        }

        public static void AreEqual(ExportStatusHistoryRecord expected, ExportStatusHistoryRecord actual)
        {
            Assert.AreEqual(expected.Completed, actual.Completed);
            Assert.AreEqual(expected.Error, actual.Error);
            Assert.AreEqual(expected.ExportId, actual.ExportId);
            Assert.AreEqual(expected.StartTime, actual.StartTime);
            Assert.AreEqual(expected.EndTime, actual.EndTime);
            Assert.AreEqual(expected.DataTypes.Count, actual.DataTypes.Count);
            for(int i=0; i<expected.DataTypes.Count; i++)
            {
                Assert.AreEqual(expected.DataTypes[i], actual.DataTypes[i]);
            }
            Assert.AreEqual(expected.ExportId, actual.ExportId);
            Assert.AreEqual(expected.RequestedAt, actual.RequestedAt);
        }
    }
}
