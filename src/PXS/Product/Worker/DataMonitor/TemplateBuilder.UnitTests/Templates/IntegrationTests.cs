// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.UnitTests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class IntegrationTests
    {
        private const string Source =
@"
<b>[[<var s:$.HelloThere var>]] [[<var s:$.PlayerOne d:Bailers var>]]</b>
<p>
This is gibberish.  You have been warned.
</p>
<p>
Now is the time for all good dogs to come and play fetch with their humans.  Each time that 
a dog does this [[<var s:'$.DogCount' f:n0 var>]] go 'bark bark bark', which tells us that 
dogs like to bark and jump around.
</p>
<p>
These are the names for a good dog: 
<ul>
[[<foreach sel:'$.GoodDogs' foreach>]]
<li>[[<var s:'$' var>]]</li>
[[<foreachend>]]
</ul>
</p>
<p>
My favorite dogs are:
<table>
[[<foreach sel:'$.FaveDogs' foreach>]]
<tr>
<td>[[<var s:'$.Breed' var>]]</td>
<td>
[[<foreach sel:'$.Examples' sep:'<br/>' foreach>]]
[[<var s:'$.Name' var>]] ([[<var s:'$.Date' f:'yyyyMMdd HHmmss' var>]])
[[<foreachend>]]
</td>
</tr>
[[<foreachend>]]
</table>
";
        private const string InputJson =
@"{
    ""DogBasics"": { ""Count"": 10000, ""DogList"": [ ""Barney"", ""Fluffy"", ""Toby"", ""Bailey"", ""Lulu"", ""Maddoch"" ] },
    ""DogDetails"":
    {
        ""Data"": 
        [
            {
                ""Breed"": ""Yellow Labrador Retriever"",
                ""Examples"": 
                [
                    { ""Name"": ""Toby"", ""Nickname"": ""Tobers"", ""Date"": ""2006-04-15T15:00:00Z"" },
                    { ""Name"": ""Lulu"", ""Nickname"": ""Loops"", ""Date"": ""2015-05-01T14:00:00Z"" }
                ]
            },
            {
                ""Breed"": ""Black Labrador Retriever"",
                ""Examples"": 
                [
                    { ""Name"": ""Bailey"", ""Nickname"": ""Bails"", ""Date"": ""2015-02-01T11:00:00Z"" }
                ]
            },
            {
                ""Breed"": ""Scottish Terrier"",
                ""Examples"": 
                [
                    { ""Name"": ""Maddoch"", ""Nickname"": ""Maddy"", ""Date"": ""2011-03-01T10:00:00Z"" }
                ]
            },
            {
                ""Breed"": ""Big White Dog"",
                ""Examples"": 
                [
                    { ""Name"": ""Fluffy"", ""Nickname"": ""Fluffers"", ""Date"": ""1991-02-01T13:00:00Z"" }
                ]
            },
            {
                ""Breed"": ""St. Bernard"",
                ""Examples"": 
                [
                    { ""Name"": ""Barney"", ""Nickname"": ""Barners"", ""Date"": ""1979-11-15T15:00:00Z"" }
                ]
            }
        ]
    }
}";

        private const string Expected =
            "<b>It's TOBERS!!! Bailers</b><p>This is gibberish.  You have been warned.</p><p>Now is the time for all good dogs " +
            "to come and play fetch with their humans.  Each time that a dog does this 10,000 go 'bark bark bark', which tells " +
            "us that dogs like to bark and jump around.</p><p>These are the names for a good dog: <ul><li>Barney</li><li>Fluffy" +
            "</li><li>Toby</li><li>Bailey</li><li>Lulu</li><li>Maddoch</li></ul></p><p>My favorite dogs are:<table><tr><td>Yellow " +
            "Labrador Retriever</td><td>Toby (20060415 150000)<br/>Lulu (20150501 140000)</td></tr><tr><td>Black Labrador " +
            "Retriever</td><td>Bailey (20150201 110000)</td></tr><tr><td>Scottish Terrier</td><td>Maddoch (20110301 100000)</td>" +
            "</tr><tr><td>Big White Dog</td><td>Fluffy (19910201 130000)</td></tr><tr><td>St. Bernard</td><td>Barney (19791115" +
            " 150000)</td></tr></table>";

        private static readonly TemplateDef DoggersTemplate = new TemplateDef
        {
            Tag = "Doggers",
            Text = IntegrationTests.Source.Replace("\n", string.Empty).Replace("\r", string.Empty),
        };

        private readonly Mock<ITemplateAccessor> mockRetriever = new Mock<ITemplateAccessor>();
        private readonly Mock<ITelemetryLogger> mockTelemetryLogger = new Mock<ITelemetryLogger>();
        private readonly Mock<ICounterFactory> mockCounterFactory = new Mock<ICounterFactory>();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();

        private readonly IUnityContainer container = new UnityContainer();

        private object dataModel;

        [TestInitialize]
        public void Init()
        {
            this.dataModel = JsonConvert.DeserializeObject<object>(IntegrationTests.InputJson);

            this.container.RegisterInstance(this.mockTelemetryLogger.Object);
            this.container.RegisterInstance(this.mockCounterFactory.Object);
            this.container.RegisterInstance<ILogger>(new MockLogger());
            this.container.RegisterInstance<IClock>(this.mockClock.Object);
            this.container.RegisterInstance<IUnityContainer>(this.container);

            Microsoft.PrivacyServices.Common.ContextModelCommon.Setup.UnitySetup.RegisterAssemblyTypes(this.container);
            Microsoft.PrivacyServices.Common.TemplateBuilder.Setup.UnitySetup.RegisterAssemblyTypes(this.container);

            this.mockRetriever.Setup(o => o.RetrieveTemplatesAsync()).ReturnsAsync(new[] { IntegrationTests.DoggersTemplate });

            this.container.RegisterInstance<ITemplateAccessor>(this.mockRetriever.Object);
        }

        [TestMethod]
        public async Task CanSucessfullyParseAndApplyModelToStoreTemplate()
        {
            TemplateRef Ref = new TemplateRef
            {
                TemplateTag = "Doggers",
                Parameters = new Dictionary<string, ModelValue>
                {
                    { "HelloThere", new ModelValue { Const = "It's TOBERS!!!" } },
                    { "DogCount", new ModelValue { Select = "$.DogBasics.Count", Const = "99999" } },
                    { "GoodDogs", new ModelValue { Select = "$.DogBasics.DogList" } },
                    { "FaveDogs", new ModelValue { Select = "$.DogDetails.Data" } },
                }
            };

            IContextFactory contextFactory = this.container.Resolve<IContextFactory>();
            ITemplateStore store = this.container.Resolve<ITemplateStore>();
            IContext context = contextFactory.Create<IContext>("Test");

            string text;
            
            await store.RefreshAsync(context, false);

            // test
            text = store.Render(context, Ref, this.dataModel);

            // verify
            Assert.AreEqual(IntegrationTests.Expected, text);
        }

        [TestMethod]
        public void CanSucessfullyParseAndApplyModelToInlineTemplate()
        {
            TemplateRef Ref = new TemplateRef
            {
                Inline = IntegrationTests.Source.Replace("\n", string.Empty).Replace("\r", string.Empty),
                Parameters = new Dictionary<string, ModelValue>
                {
                    { "HelloThere", new ModelValue { Const = "It's TOBERS!!!" } },
                    { "DogCount", new ModelValue { Select = "$.DogBasics.Count", Const = "99999" } },
                    { "GoodDogs", new ModelValue { Select = "$.DogBasics.DogList" } },
                    { "FaveDogs", new ModelValue { Select = "$.DogDetails.Data" } },
                }
            };

            IContextFactory contextFactory = this.container.Resolve<IContextFactory>();
            ITemplateStore store = this.container.Resolve<ITemplateStore>();
            IContext context = contextFactory.Create<IContext>("Test");
            string text;

            // test
            text = store.Render(context, Ref, this.dataModel);

            // verify
            Assert.AreEqual(IntegrationTests.Expected, text);
        }
    }
}
