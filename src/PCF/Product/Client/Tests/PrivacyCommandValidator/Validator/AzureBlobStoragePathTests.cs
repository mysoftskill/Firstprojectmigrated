namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    
    /// <summary>
    /// Export command blob storage validation tests
    /// </summary>
    [TestClass]
    public class AzureBlobStoragePathTests : ValidatorTestBase
    {
        #region AzureBlobStoragePathTests

        /// <summary>
        /// Same uri in both token and claim
        /// </summary>
        [TestMethod]
        public async Task CommandAndValidatorHaveSameBlobUri()
        {
            string commandStorageUri = "https://notrealstorage.blob.core.windows.net/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";
            string tokenStorageUri = commandStorageUri;

            await this.ValidateStorageUriE2EAsync(commandStorageUri, tokenStorageUri);
        }

        /// <summary>
        /// Token storage uri is a base of command storage uri
        /// </summary>
        [TestMethod]
        public async Task CommandBlobIsMoreSpecific()
        {
            string commandStorageUri = "https://x.y.z.windows.net/container/blob/";
            string tokenStorageUri = "https://x.y.z.windows.net/container/";

            await this.ValidateStorageUriE2EAsync(commandStorageUri, tokenStorageUri);
        }

        /// <summary>
        /// Token storage uri port does not match command one
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidAzureStorageUriException))]
        public async Task TokenStorageUriNotFound()
        {
            string commandStorageUri = "https://notrealstorage.blob.core.windows.net/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";
            string tokenStorageUri = null;

            try
            {
                await this.ValidateStorageUriE2EAsync(commandStorageUri, tokenStorageUri);
            }
            catch (InvalidAzureStorageUriException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("storage uri claim not found"));
                throw;
            }
        }

        /// <summary>
        /// Token storage uri port does not match command one
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidAzureStorageUriException))]
        public async Task DoesNotMatchPort_TokenHttpsVsCommandCustomHttpsPort()
        {
            string commandStorageUri    = "https://notrealstorage.blob.core.windows.net:666/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";
            string tokenStorageUri      = "https://notrealstorage.blob.core.windows.net/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";

            try
            {
                await this.ValidateStorageUriE2EAsync(commandStorageUri, tokenStorageUri);
            }
            catch (InvalidAzureStorageUriException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("command storage uri port"));
                throw;
            }
        }

        /// <summary>
        /// Token storage uri port does not match command one
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidAzureStorageUriException))]
        public async Task DoesNotMatchPort_TokenCustomHttpVsCommandDefaultHttp()
        {
            string commandStorageUri    = "https://notrealstorage.blob.core.windows.net/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";
            string tokenStorageUri      = "https://notrealstorage.blob.core.windows.net:666/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";

            try
            {
                await this.ValidateStorageUriE2EAsync(commandStorageUri, tokenStorageUri);
            }
            catch (InvalidAzureStorageUriException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("command storage uri port"));
                throw;
            }
        }

        /// <summary>
        /// Token storage uri scheme does not match command one
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidAzureStorageUriException))]
        public async Task DoesNotMatchSceme_01()
        {
            string commandStorageUri = "http://notrealstorage.blob.core.windows.net/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";
            string tokenStorageUri = "https://notrealstorage.blob.core.windows.net/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";

            try
            {
                await this.ValidateStorageUriE2EAsync(commandStorageUri, tokenStorageUri);
            }
            catch (InvalidAzureStorageUriException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("command storage uri schema"));
                throw;
            }
        }

        /// <summary>
        /// Token storage uri scheme does not match command one
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidAzureStorageUriException))]
        public async Task DoesNotMatchSceme_02()
        {
            string commandStorageUri = "https://notrealstorage.blob.core.windows.net/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";
            string tokenStorageUri = "http://notrealstorage.blob.core.windows.net/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";

            try
            {
                await this.ValidateStorageUriE2EAsync(commandStorageUri, tokenStorageUri);
            }
            catch (InvalidAzureStorageUriException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("command storage uri schema"));
                throw;
            }
        }

        /// <summary>
        /// Token storage uri is not a base of command storage uri
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidAzureStorageUriException))]
        public async Task IsNotBase_01()
        {
            string commandStorageUri = "https://notrealstorage.blob.core.windows.net/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";
            string tokenStorageUri = "https://extra.notrealstorage.blob.core.windows.net/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";

            try
            {
                await this.ValidateStorageUriE2EAsync(commandStorageUri, tokenStorageUri);
            }
            catch (InvalidAzureStorageUriException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("is not the base uri"));
                throw;
            }
        }

        /// <summary>
        /// Token storage uri is not a base of command storage uri
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidAzureStorageUriException))]
        public async Task IsNotBase_02()
        {
            string commandStorageUri = "https://notrealstorage.blob.core.windows.net/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";
            string tokenStorageUri = "https://notrealstorage.blob.core.windows.extra.net/test1?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";

            try
            {
                await this.ValidateStorageUriE2EAsync(commandStorageUri, tokenStorageUri);
            }
            catch (InvalidAzureStorageUriException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("is not the base uri"));
                throw;
            }
        }

        /// <summary>
        /// Token storage uri is not a base of command storage uri
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidAzureStorageUriException))]
        public async Task IsNotBase_03()
        {
            string commandStorageUri = "https://notrealstorage.blob.core.windows.net/c2/c3?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";
            string tokenStorageUri = "https://notrealstorage.blob.core.windows.net/c1/c3?sv=2017-04-17&sr=c&sig=4gh%2FjBI7tB%2FU2LP%2Ba0i65L%2BHJZy2qlSUunxXhl17VgQ%3D&se=2018-04-22T19%3A00%3A38Z&sp=acw";

            try
            {
                await this.ValidateStorageUriE2EAsync(commandStorageUri, tokenStorageUri);
            }
            catch (InvalidAzureStorageUriException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("is not the base uri"));
                throw;
            }
        }

        #endregion

        private async Task ValidateStorageUriE2EAsync(string commandStorageUri, string tokenStorageUri)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new DeviceSubject
                {
                    GlobalDeviceId = long.MaxValue
                },
                AzureBlobContainerTargetUri = tokenStorageUri == null ? null : new Uri(tokenStorageUri)
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // override command blob uri
            claims.AzureBlobContainerTargetUri = new Uri(commandStorageUri);

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }
    }
}
