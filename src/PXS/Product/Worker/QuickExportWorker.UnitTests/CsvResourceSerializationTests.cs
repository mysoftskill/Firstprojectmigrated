// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.UnitTests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.CsvSerializer;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CsvResourceSerializationTests
    {
        private ISerializer serializer;
        private DateTimeOffset datetime;
        private DateTimeOffset endtime;
        private List<string> header;
        private TimeSpan timespan;

        //  These tests are designed to test the serialization of each resource type into CSV
        //  There are a couple headers added into each test to help with debugging, and there is an example test case to output a file
        [TestInitialize]
        public void Setup()
        {
            serializer = new CsvSerializer();
            datetime = new DateTimeOffset(new DateTime(2020, 3, 14, 0, 0, 0, DateTimeKind.Utc));
            endtime = new DateTimeOffset(new DateTime(2021, 3, 14, 0, 0, 0, DateTimeKind.Utc));
            timespan = new TimeSpan(1, 2, 3);
        }

        [TestMethod]
        public void SerializeAppUsage()
        {
            AppUsageResource resource = new AppUsageResource()
            {
                DateTime = datetime,
                EndDateTime = endtime,
                DeviceId = null,
                Aggregation = "Daily",
                AppName = "Test AppName",
                AppPublisher = "Test Publisher"
            };

            //Write To CSV and compare with expected formatting
            string output = serializer.ConvertResource(resource);

            Assert.AreEqual("\"3/14/2020 12:00:00 AM +00:00\",\"3/14/2021 12:00:00 AM +00:00\",,Daily,Test AppName,Test Publisher" + Environment.NewLine, output);

            resource.DeviceId = "g:6966508624985115";

            //Run test with actual DeviceId
            output = serializer.ConvertResource(resource);

            Assert.AreEqual("\"3/14/2020 12:00:00 AM +00:00\",\"3/14/2021 12:00:00 AM +00:00\",g:6966508624985115,Daily,Test AppName,Test Publisher" + Environment.NewLine, output);
        }

        [TestMethod]
        public void SerializeBrowse()
        {
            BrowseResource resource = new BrowseResource()
            {
                DateTime = datetime,
                DeviceId = "g:6966508624985115",
                NavigatedToUrl = "https://test.com",
                PageTitle = "Test Page Title",
                SearchTerms = null
            };

            header = new List<string>();
            header.Add("DateTime");
            header.Add("DeviceId");
            header.Add("NavigatedToUrl");
            header.Add("PageTitle");
            header.Add("SearchTerms");
            string output = serializer.ConvertResource(resource);

            Assert.AreEqual("\"3/14/2020 12:00:00 AM +00:00\",g:6966508624985115,\"https://test.com\",Test Page Title," + Environment.NewLine, output);
        }

        [TestMethod]
        public void SerializeLocation()
        {
            LocationResource resource = new LocationResource()
            {
                DateTime = datetime,
                DeviceId = "",
                AccuracyRadius = 55.0,
                ActivityType = null,
                Address = null,
                DeviceType = LocationDeviceType.PC,
                Distance = 0,
                EndDateTime = endtime,
                Latitude = 47.1994265055587,
                Longitude = -122.908254018604,
                Name = "",
                Url = ""
            };

            header = new List<string>();
            header.Add("DateTime");
            header.Add("DeviceId");
            header.Add("AccuracyRadius");
            header.Add("ActivityType");
            header.Add("AddressLine1");
            header.Add("AddressLine2");
            header.Add("AddressLine3");
            header.Add("CountryRegion");
            header.Add("FormattedAddress");
            header.Add("Locality");
            header.Add("PostalCode");
            header.Add("DeviceType");
            header.Add("Distance");
            header.Add("EndDateTime");
            header.Add("Latitutde");
            header.Add("Longitude");
            header.Add("Name");
            header.Add("Url");
            string output = serializer.ConvertResource(resource);

            Assert.AreEqual("\"3/14/2020 12:00:00 AM +00:00\",,55,,,,,,,,,PC,0,\"3/14/2021 12:00:00 AM +00:00\",47.1994265055587,-122.908254018604,,," + Environment.NewLine, output);
        }

        [TestMethod]
        public void SerializeSearch()
        {
            SearchResource resource = new SearchResource
            {
                DateTime = datetime,
                DeviceId = null,
                Location = null
            };

            IList<NavigatedToUrlResource> searchlist = new List<NavigatedToUrlResource>();
            var url1 = new NavigatedToUrlResource()
            {
                Time = datetime,
                Title = "Test Title 1",
                Url = "https://test1.com"
            };

            searchlist.Add(url1);
            resource.NavigatedToUrls = searchlist;
            resource.SearchTerms = "test search";

            string output = serializer.ConvertResource(resource);

            header = new List<string>();
            header.Add("DateTime");
            header.Add("DeviceId");
            header.Add("AccuracyRadius");
            header.Add("Latitude");
            header.Add("Longitude");
            header.Add("Time");
            header.Add("Title");
            header.Add("Url");
            header.Add("SearchTerms");

            //Test write this file
            Assert.AreEqual("\"3/14/2020 12:00:00 AM +00:00\",,,,,\"3/14/2020 12:00:00 AM +00:00\"," +
                "Test Title 1,\"https://test1.com\",test search" + Environment.NewLine, output);

            //Run this test again
            resource.NavigatedToUrls = null;
            output = serializer.ConvertResource(resource);

            Assert.AreEqual("\"3/14/2020 12:00:00 AM +00:00\",,,,,,,,test search" + Environment.NewLine, output);

            List<NavigatedToUrlResource> urlList = new List<NavigatedToUrlResource>();
            resource.NavigatedToUrls = urlList;
            output = serializer.ConvertResource(resource);

            Assert.AreEqual("\"3/14/2020 12:00:00 AM +00:00\",,,,,,,,test search" + Environment.NewLine, output);
        }

        //This test condition verifies if multiple urls are passed
        [TestMethod]
        public void SerializeSearchMultipleUrls()
        {
            IList<NavigatedToUrlResource> searchlist = new List<NavigatedToUrlResource>();

            searchlist.Add(new NavigatedToUrlResource()
            {
                Time = datetime,
                Title = "Test Title 1",
                Url = "https://test1.com"
            });

            searchlist.Add(new NavigatedToUrlResource()
            {
                Time = datetime,
                Title = "Test Title 2",
                Url = "https://test2.com"
            });

            SearchResource resource = new SearchResource()
            {
                DateTime = datetime,
                DeviceId = null,
                Location = null,
                NavigatedToUrls = searchlist,
                SearchTerms = "test search"
            };

            string output = serializer.ConvertResource(resource);

            header = new List<string>();
            header.Add("DateTime");
            header.Add("DeviceId");
            header.Add("AccuracyRadius");
            header.Add("Latitude");
            header.Add("Longitude");
            header.Add("Time");
            header.Add("Title");
            header.Add("Url");
            header.Add("SearchTerms");

            Assert.AreEqual("\"3/14/2020 12:00:00 AM +00:00\",,,,,\"3/14/2020 12:00:00 AM +00:00\",Test Title 1,\"https://test1.com\",test search\r\n\"3/14/2020 12:00:00 AM +00:00" +
                "\",,,,,\"3/14/2020 12:00:00 AM +00:00\",Test Title 2,\"https://test2.com\",test search" + Environment.NewLine, output);
        }

        [TestMethod]
        public void SerializeVoice()
        {
            VoiceResource resource = new VoiceResource()
            {
                DateTime = datetime,
                DeviceId = null,
                Application = "Cortana",
                DeviceType = "Desktop Test",
                DisplayText = "DisplayText Test"
            };

            string output = serializer.ConvertResource(resource);

            header = new List<string>();
            header.Add("DateTime");
            header.Add("DeviceId");
            header.Add("Application");
            header.Add("DeviceType");
            header.Add("DisplayText");

            Assert.AreEqual("\"3/14/2020 12:00:00 AM +00:00\",,Cortana,Desktop Test,DisplayText Test" + Environment.NewLine, output);
        }

        [TestMethod]
        public void SerializeContentConsumption()
        {
            ContentConsumptionResource resource = new ContentConsumptionResource()
            {
                DateTime = datetime,
                DeviceId = null,
                AppName = "Test AppName",
                Artist = "Test Artist",
                ConsumptionTime = timespan,
                ContainerName = "Test ContainerName",
                ContentUrl = new Uri("https://content.com"),
                IconUrl = new Uri("https://icon.com"),
                MediaType = ContentConsumptionResource.ContentType.Song,
                Title = "Test Title"
            };

            string output = serializer.ConvertResource(resource);

            header = new List<string>();
            header.Add("DateTime");
            header.Add("DeviceId");
            header.Add("AppName");
            header.Add("Artist");
            header.Add("ConsumptionTime");
            header.Add("ContainerName");
            header.Add("ContentUrl");
            header.Add("IconUrl");
            header.Add("MediaType");
            header.Add("Title");

            Assert.AreEqual("\"3/14/2020 12:00:00 AM +00:00\",,Test AppName" +
                ",Test Artist,01:02:03,Test ContainerName,\"https://content.com/\",\"https://icon.com/\",Song,Test Title" + Environment.NewLine, output);
        }
        [TestMethod]
        public void NullConsumptionResource()
        {
            ContentConsumptionResource resource = new ContentConsumptionResource()
            {
                DateTime = datetime,
                DeviceId = null,
                AppName = "\"Test DoubleQuotes\"",
                Artist = "Test\"ContainingQuote",
                ConsumptionTime = timespan,
                ContainerName = "Test\n NewLine",
                ContentUrl = null,
                IconUrl = null,
                MediaType = ContentConsumptionResource.ContentType.Song,
                Title = "Test Title"
            };
            string output = serializer.ConvertResource(resource);
            Assert.AreEqual("\"3/14/2020 12:00:00 AM +00:00\",,\"\"\"Test DoubleQuotes\"\"\"" +
                ",\"Test\"\"ContainingQuote\",01:02:03,\"Test  NewLine\",,,Song,Test Title\r\n", output);
        }
        [TestMethod]
        public void MessyContentConsumption()
        {
            ContentConsumptionResource resource = new ContentConsumptionResource()
            {
                DateTime = datetime,
                DeviceId = null,
                AppName = "\"Test DoubleQuotes\"",
                Artist = "Test\"ContainingQuote",
                ConsumptionTime = timespan,
                ContainerName = "Test\n NewLine",
                ContentUrl = new Uri("https://content.com"),
                IconUrl = new Uri("https://icon.com"),
                MediaType = ContentConsumptionResource.ContentType.Song,
                Title = "Test Title"
            };

            string output = serializer.ConvertResource(resource);

            Assert.AreEqual("\"3/14/2020 12:00:00 AM +00:00\",,\"\"\"Test DoubleQuotes\"\"\",\"Test\"\"ContainingQuote\",01:02:03,\"Test  NewLine\"" +
                ",\"https://content.com/\",\"https://icon.com/\",Song,Test Title\r\n", output);
        }
    }
}