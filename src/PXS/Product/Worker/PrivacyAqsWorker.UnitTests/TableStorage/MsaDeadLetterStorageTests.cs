// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.TableStorage
{
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.TableStorage;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    [TestClass]
    public class MsaDeadLetterStorageTests
    {
        [TestMethod]
        public void ShouldSerializeData()
        {
            var storage = new MsaDeadLetterStorage(12)
            {
                DataActual = new MsaAccountDeadLetterInformation
                {
                    Cid = 1,
                    EventType = MsaAccountEventType.AccountClose,
                    Puid = 12
                }
            };

            string obj = JsonConvert.SerializeObject(storage);
            Assert.IsNotNull(obj);

            var deserialized = JsonConvert.DeserializeObject<MsaDeadLetterStorage>(obj);
            Assert.IsNotNull(deserialized);
        }
    }
}
