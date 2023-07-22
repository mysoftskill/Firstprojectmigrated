namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ValidationServiceTests : ValidatorTestBase
    {
        [TestMethod]
        public void VerifierMinterStarts()
        {
            VerifierTokenMinter.MintVerifier(Enumerable.Empty<Claim>(), MsaIssuerUrl, MsaTestSigningCredentials);
        }

        [TestMethod]
        public async Task EnsureValidAsync_NullVerifierInTest_Suceeds()
        {
            await this.TestValidationService.EnsureValidAsync(null, new CommandClaims(), CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task EnsureValidAsync_NullVerifierInProduction_ThrowsArgumentException()
        {
            await this.ProductionValidationService.EnsureValidAsync(null, new CommandClaims(), CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task EnsureValidAsync_NoCommand_ThrowsArgumentException()
        {
            await this.ProductionValidationService.EnsureValidAsync("verifier", null, CancellationToken.None);
            await this.TestValidationService.EnsureValidAsync("verifier", null, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task EnsureValidAsync_NoSubject_ThrowsArgumentException()
        {
            await this.ProductionValidationService.EnsureValidAsync("verifier", new CommandClaims(), CancellationToken.None);
            await this.TestValidationService.EnsureValidAsync("verifier", new CommandClaims(), CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_InvalidSubjectType_ThrowsInvalidPrivacyCommandException()
        {
            await this.TestValidationService.EnsureValidAsync("verifier", new CommandClaims { Subject = new Mock<IPrivacySubject>().Object }, CancellationToken.None);
        }

        [TestMethod]
        public async Task EnsureValidAsync_EmptyDemographicSubjectNullVerifier_Suceeds()
        {
            await this.ProductionValidationService.EnsureValidAsync(null, new CommandClaims { Subject = new DemographicSubject() }, CancellationToken.None);
            await this.TestValidationService.EnsureValidAsync(null, new CommandClaims { Subject = new DemographicSubject() }, CancellationToken.None);
        }

        [TestMethod]
        public async Task EnsureValidAsync_EmptyMicrosoftEmployeeNullVerifier_Suceeds()
        {
            await this.ProductionValidationService.EnsureValidAsync(null, new CommandClaims { Subject = new MicrosoftEmployee() }, CancellationToken.None);
            await this.TestValidationService.EnsureValidAsync(null, new CommandClaims { Subject = new MicrosoftEmployee() }, CancellationToken.None);
        }

        [TestMethod]
        public async Task EnsureValidAsync_EmptyEdgeBrowserSubjectNullVerifier_Suceeds()
        {
            await this.ProductionValidationService.EnsureValidAsync(null, new CommandClaims { Subject = new EdgeBrowserSubject() }, CancellationToken.None);
            await this.TestValidationService.EnsureValidAsync(null, new CommandClaims { Subject = new EdgeBrowserSubject() }, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task EnsureValidAsync_EmptyMsaSubjectNullVerifier_ThrowsArgumentException()
        {
            await this.ProductionValidationService.EnsureValidAsync(null, new CommandClaims { Subject = new MsaSubject() }, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task EnsureValidAsync_EmptyAadSubjectNullVerifier_ThrowsArgumentException()
        {
            await this.ProductionValidationService.EnsureValidAsync(null, new CommandClaims { Subject = new AadSubject() }, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task EnsureValidAsync_EmptyDeviceSubjectNullVerifier_ThrowsArgumentException()
        {
            await this.ProductionValidationService.EnsureValidAsync(null, new CommandClaims { Subject = new DeviceSubject() }, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_VerifierFailsPrechecks_ThrowsInvalidPrivacyCommandException()
        {
            const string verifier =
                "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6IjFIWDAzOXJEOHc1d1lmd01xSlRKVjRCSHJURSJ9.eyJ2ZXIiOjEsInRpZCI6IjQ5MjUzMDhjLWYxNjQtNGQyZC1iYzdlLTA2MzExMzJlOTM3NSIsImlzcyI6Imh0dHBzOi8vZ2Rwci5sb2dpbi5saXZlLWludC5jb20vNDkyNTMwOGMtZjE2NC00ZDJkLWJjN2UtMDYzMTEzMmU5Mzc1IiwiZXhwIjoxNTE3MzU2NDQ2LCJuYmYiOjE1MTIxNzI0NDYsIm9wIjoiRXhwb3J0IiwianRpIjoiZTQ5NDQxMDQtMGJmNS00M2M5LTI3M2YtZTA5MTA0MWE4YWQxIiwib3BfdGltZSI6MTUxMjE3MjQ0NiwicHVpZCI6IjAwMDMwMDAwOTgzRkVFQzQiLCJyZXAiOjAsImNpZCI6ImI1NGNjOTNlMDkwNDlmZGMiLCJhbmlkIjoiQzQzMTMwMEIyNDRGRDI3NkFCREYzNDUwRkZGRkZGRkYiLCJ4dWlkIjoidGhpcy1pcy1hLXh1aWQtaWQiLCJwcmVkIjoiQnJvd3Nlckhpc3Rvcnk7TG9jYXRpb25EYXRhO09wYXF1ZUlkIn0.pCP8K6MyLBnugmrQIKzuG1DDoYUzgaoIT4-NjVg5kvURcYM4bFdE8wk2zO2eSjN0ccHFuvZI299pzs-XdHzJmnRcUx3lHSEu7GVbHpo-NA1JQda9mbAyzSH-4ro3tpY-BfyMMLP02SDSTBPnBv8V7qtpQzkklFtMkS5oTWh7j97EnkFYuI27l7PP6w8LAcJ-uVwiuai0SRjXh585WbDgz_YkofIP6F_fte2AMgCP1ISuhd4relUVghni0eb365eC7zODk5C3nly-WyOWVNr6B4Lu2p08wYZGlCypFtYRvby8noZanjChG6PWaRj2szq_T2zDyzarUW02ha2xKo31AA";

            await this.TestValidationService.EnsureValidAsync(verifier, new CommandClaims { Subject = new DeviceSubject() }, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task EnsureValidAsync_NullCommandClaims_ThrowsArgumentException()
        {
            var claims = new CommandClaims
            {
                CommandId = "123",
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), null, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_NullTokenClaims_ThrowsInvalidPrivacyCommandException()
        {
            var commandClaims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            string verifier = VerifierTokenMinter.MintVerifier(null, null, MsaTestSigningCredentials);
            await this.TestValidationService.EnsureValidAsync(verifier, commandClaims, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task EnsureValidAsync_NullSubject_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = null
            };

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims);
        }

        #region Operation and Data Types Tests
        [TestMethod]
        [DataRow("SearchRequestsAndQuery", "SearchRequestsAndQuery")]
        [DataRow("BrowsingHistory", "BrowsingHistory")]
        public async Task EnsureValidAsync_ValidScopedDeleteOperation_Suceeds(string commandDataTypesStr, string tokenDataTypesStr)
        {
            DataTypeId commandDataType = Policies.Current.DataTypes.CreateId(commandDataTypesStr);

            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.ScopedDelete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                },
                DataType = commandDataType
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);
            tokenClaims.Add(new Claim("dts", tokenDataTypesStr));
            
            // Edit command info with additional data type
            claims.Operation = ValidOperation.Delete;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_InvalidOperation_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = (ValidOperation)1000,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_InvalidScopedDeleteCommandOperation_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.ScopedDelete, // Command shouldn't have a ScopedDelete
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };
            
            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_InvalidMismatchedOperation_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Export,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Edit command info with wrong operation
            claims.Operation = ValidOperation.AgeOut;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow(null, "SearchRequestsAndQuery", "ScopedDelete operations must have a data type.")]
        [DataRow("SearchRequestsAndQuery", "", "ScopedDelete operations must have a data type.")]
        [DataRow("SearchRequestsAndQuery", "Any", "ScopedDelete operations cannot have data type 'Any'.")]
        [DataRow("Any", "SearchRequestsAndQuery", "ScopedDelete operations cannot have data type 'Any'.")]
        [DataRow("BrowsingHistory", "SearchRequestsAndQuery", "Data type mismatch. Command data type: BrowsingHistory. Token data type: SearchRequestsAndQuery.")]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_InvalidDataTypeOperation_ThrowsInvalidPrivacyCommandException(string commandDataTypesStr, string tokenDataTypesStr, string expectedExceptionText)
        {
            DataTypeId commandDataType = commandDataTypesStr == null ? null : Policies.Current.DataTypes.CreateId(commandDataTypesStr);

            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.ScopedDelete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                },
                DataType = commandDataType
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);
            tokenClaims.Add(new Claim("dts", tokenDataTypesStr));

            // Edit command info with additional data type
            claims.Operation = ValidOperation.Delete;

            try
            {
                await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
            }
            catch (Exception e)
            {
                StringAssert.Contains(e.ToString(), expectedExceptionText);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_UnknownDataTypeOperation_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.ScopedDelete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                },
                DataType = Policies.Current.DataTypes.Ids.SearchRequestsAndQuery
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Edit command info with additional data type
            claims.Operation = ValidOperation.Delete;
            tokenClaims.Add(new Claim("dts", "BadValue"));

            try
            {
                await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
            }
            catch (Exception e)
            {
                StringAssert.Contains(e.ToString(), "Could not parse token data type 'BadValue'.");
                throw;
            }
        }

        #endregion

        #region CommandIdClaimTests

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_MismatchTokenCommandId_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            claims.CommandId = Guid.NewGuid().ToString();

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_NullTokenCommandId_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = null,
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            claims.CommandId = Guid.NewGuid().ToString();

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_EmptyTokenCommandId_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = string.Empty,
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            claims.CommandId = Guid.NewGuid().ToString();

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_NotGuidTokenCommandId_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = "123",
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            claims.CommandId = Guid.NewGuid().ToString();

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_NotGuidCommandCommandId_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            claims.CommandId = "123";

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        #endregion

        #region PA and CA Tests

        [TestMethod]
        public async Task EnsureValidAsync_ProcessorApplicableMismatch()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString(),
                },
                ProcessorApplicable = true,
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Should work just fine.
            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);

            claims.ProcessorApplicable = false;

            try
            {
                // Should fail; enforcement is on and there is a mismatch.
                await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
                Assert.Fail();
            }
            catch (InvalidPrivacyCommandException)
            {
            }

            // Still OK; remove CA from token claims.
            tokenClaims = tokenClaims.Where(x => x.Type != "pa").ToList();
            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        public async Task EnsureValidAsync_ControllerApplicableMismatch()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString(),
                },
                ControllerApplicable = true,
                ProcessorApplicable = false,
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Should work just fine.
            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);

            claims.ControllerApplicable = false;

            try
            {
                // Should fail; enforcement is on and there is a mismatch.
                await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
                Assert.Fail();
            }
            catch (InvalidPrivacyCommandException)
            {
            }

            // Still OK; remove CA from token claims.
            tokenClaims = tokenClaims.Where(x => x.Type != "ca").ToList();
            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        #endregion

        #region MsaSubjectTests

        [DataTestMethod]
        [DataRow(ValidOperation.Delete)]
        [DataRow(ValidOperation.AccountClose)]
        [DataRow(ValidOperation.AgeOut)]
        [DataRow(ValidOperation.Export)]
        public async Task EnsureValidAsync_MsaSubject_ValidMsaClaims_Succeeds(ValidOperation operation)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = operation,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims);
        }

        [DataTestMethod]
        [DataRow(99)]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_MsaSubject_InvalidOperation_Fails(ValidOperation operation)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = operation,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            try
            {
                await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims);
            }
            catch (InvalidPrivacyCommandException e)
            {
                StringAssert.Contains(e.ToString(), "Failed to parse the token operation 'InvalidOperation'");
                throw;
            }
        }

        [TestMethod]
        public async Task EnsureValidAsync_MsaSubject_ValidMsaClaims_EmptyCommandAnid_Succeeds()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Anid = string.Empty;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        public async Task EnsureValidAsync_MsaSubject_ValidMsaClaims_AnidCaseInsensitive_Succeeds()
        {
            string anid = Guid.NewGuid().ToString();
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = anid,
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Anid = anid.ToUpperInvariant();

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        public async Task EnsureValidAsync_ValidMsaClaims_EmptyCommandXuid_Succeeds()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Xuid = string.Empty;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        public async Task EnsureValidAsync_ValidMsaClaims_XuidCaseInsensitive_Succeeds()
        {
            string xuid = Guid.NewGuid().ToString();
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = xuid
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Xuid = xuid.ToUpperInvariant();

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_MsaSubject_MismatchTokenAnid_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Anid = Guid.NewGuid().ToString();

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_MsaSubject_NullTokenAnid_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = null,
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Anid = Guid.NewGuid().ToString();

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_MsaSubject_EmptyTokenAnid_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.Empty.ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Anid = Guid.NewGuid().ToString();

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_MsaSubject_MismatchTokenXuid_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Xuid = Guid.NewGuid().ToString();

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_MsaSubject_NullTokenXuid_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = null
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Xuid = Guid.NewGuid().ToString();

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_MsaSubject_EmptyTokenXuid_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Xuid = Guid.NewGuid().ToString();

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_MsaSubject_MismatchTokenCid_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Cid = long.MaxValue - 1;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_MsaSubject_ZeroTokenCid_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = 0,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Cid = long.MinValue;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        public async Task EnsureValidAsync_MsaSubject_ZeroCommandCid_Succeeds()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MinValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Cid = 0;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_MsaSubject_MismatchTokenPuid_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MinValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Puid = long.MaxValue - 1;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_MsaSubject_ZeroTokenPuid_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = 0,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Puid = long.MaxValue;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        public async Task EnsureValidAsync_MsaSubject_ZeroCommandPuid_Succeeds()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new MsaSubject
                {
                    Anid = Guid.NewGuid().ToString(),
                    Cid = long.MaxValue,
                    Puid = long.MaxValue,
                    Xuid = Guid.NewGuid().ToString()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((MsaSubject)claims.Subject).Puid = 0;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        #endregion

        #region AadSubjectTests

        [TestMethod]
        public async Task EnsureValidAsync_ValidAadClaims_Succeeds()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue
                }
            };

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject), claims);
        }

        [TestMethod]
        public async Task EnsureValidAsync_AadSubject_EmptyCommandTenantId_Succeeds()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject
                {
                    TenantId = Guid.NewGuid(),
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo);

            // Set command info
            ((AadSubject)claims.Subject).TenantId = Guid.Empty;

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_AadSubject_MismatchTenantId_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject
                {
                    TenantId = Guid.NewGuid(),
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo);

            // Set command info
            ((AadSubject)claims.Subject).TenantId = Guid.NewGuid();

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject), claims, tokenClaims);
        }

        [TestMethod]
        public async Task EnsureValidAsync_AadSubject_EmptyObjectId_Succeeds()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo);

            // Set command info
            ((AadSubject)claims.Subject).ObjectId = Guid.Empty;

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_AadSubject_MismatchObjectId_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo);

            // Set command info
            ((AadSubject)claims.Subject).ObjectId = Guid.NewGuid();

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_AadSubject_MismatchTokenOrgIdPUID_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo);

            // Set command info
            ((AadSubject)claims.Subject).OrgIdPUID = long.MaxValue - 1;

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_AadSubject_ZeroTokenOrgIdPUID_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = 0
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo);

            // Set command info
            ((AadSubject)claims.Subject).OrgIdPUID = long.MaxValue;

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject), claims, tokenClaims);
        }

        [TestMethod]
        public async Task EnsureValidAsync_AadSubject_ZeroCommandOrgIdPUID_Succeeds()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo);

            // Set command info
            ((AadSubject)claims.Subject).OrgIdPUID = 0;

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject), claims, tokenClaims);
        }

        #endregion

        #region AADSubject2

        [TestMethod]
        [DataRow("2.0")]
        [DataRow("3.0")]
        public async Task EnsureValidAsync_AadSubject2_Home_Succeeds(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Home,
                    HomeTenantId = AadTid
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("2.0")]
        [DataRow("3.0")]
        public async Task EnsureValidAsync_AadSubject2_Home_EmptyHomeTenantId_Succeeds(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = Guid.Empty,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Home,
                    HomeTenantId = Guid.Empty
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            // Set command info
            ((AadSubject2)claims.Subject).HomeTenantId = Guid.Empty;

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("2.0")]
        [DataRow("3.0")]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_AadSubject2_Home_MisMatchHomeTenantId_Fails(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = Guid.NewGuid(), // TenantId is different from HomeTenant Id, this is an error
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Home,
                    HomeTenantId = Guid.NewGuid()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("2.0")]
        [DataRow("3.0")]
        public async Task EnsureValidAsync_AadSubject2_Home_EmptyTenantId_Succeeds(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Home,
                    HomeTenantId = AadTid
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            // Set command info
            ((AadSubject2)claims.Subject).TenantId = Guid.Empty;

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("2.0")]
        [DataRow("3.0")]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_AadSubject2_Home_MisMatchTenantId_Fails(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Home,
                    HomeTenantId = AadTid
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            // Set command info
            ((AadSubject2)claims.Subject).TenantId = Guid.NewGuid();

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("2.0")]
        [DataRow("3.0")]
        public async Task EnsureValidAsync_AadSubject2_Home_EmptyObjectId_Succeeds(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Home,
                    HomeTenantId = AadTid
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            // Set command info
            ((AadSubject2)claims.Subject).ObjectId = Guid.Empty;

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("2.0")]
        [DataRow("3.0")]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_AadSubject2_Home_MisMatchObjectId_Fails(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Home,
                    HomeTenantId = AadTid
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            // Set command info
            ((AadSubject2)claims.Subject).ObjectId = Guid.NewGuid();

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("2.0")]
        [DataRow("3.0")]
        public async Task EnsureValidAsync_AadSubject2_Home_ZeroOrgIdPUID_Succeeds(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Home,
                    HomeTenantId = AadTid
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            // Set command info
            ((AadSubject2)claims.Subject).OrgIdPUID = 0;

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("2.0")]
        [DataRow("3.0")]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_AadSubject2_Home_MisMatchOrgIdPuid_Fails(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Home,
                    HomeTenantId = AadTid
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            // Set command info
            ((AadSubject2)claims.Subject).OrgIdPUID = long.MaxValue - 1;

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("3.0")]
        public async Task EnsureValidAsync_AadSubject2_Resource_Succeeds(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Resource,
                    HomeTenantId = Guid.NewGuid()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("3.0")]
        public async Task EnsureValidAsync_AadSubject2_ResourceWithZeroValues_Succeeds(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = AadTid,
                    ObjectId = Guid.Empty,
                    OrgIdPUID = 0,
                    TenantIdType = TenantIdType.Resource,
                    HomeTenantId = Guid.NewGuid()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("3.0")]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_AadSubject2_Resource_EmptyHomeTenantId_Fails(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Resource,
                    HomeTenantId = Guid.Empty
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            // Set command info
            ((AadSubject2)claims.Subject).HomeTenantId = Guid.Empty;

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("3.0")]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_AadSubject2_Resource_MisMatchHomeTenantId_Fails(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Resource,
                    HomeTenantId = Guid.NewGuid()
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            // Set command info
            ((AadSubject2)claims.Subject).HomeTenantId = Guid.NewGuid();

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        [TestMethod]
        [DataRow("2.0")]
        [DataRow("3.0")]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_AadSubject2_MisMatchTenantIdTypeFails(string verifierVersion)
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new AadSubject2
                {
                    TenantId = AadTid,
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = long.MaxValue,
                    TenantIdType = TenantIdType.Home,
                    HomeTenantId = AadTid
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(AadTestKeyId, SigningAlgo, verifierVersion);

            // Set command info
            ((AadSubject2)claims.Subject).TenantIdType = TenantIdType.Resource;

            await this.EnsureValidAsyncEndToEnd(typeof(AadSubject2), claims, tokenClaims);
        }

        #endregion

        #region DeviceSubjectTests

        [TestMethod]
        public async Task EnsureValidAsync_DeviceSubject_ValidSubject_Succeeds()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new DeviceSubject
                {
                    GlobalDeviceId = long.MaxValue
                }
            };

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims);
        }

        [TestMethod]
        public async Task EnsureValidAsync_DeviceSubject_ZeroCommandGlobalDeviceId_Succeeds()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new DeviceSubject
                {
                    GlobalDeviceId = long.MaxValue
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((DeviceSubject)claims.Subject).GlobalDeviceId = 0;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_DeviceSubject_ZeroTokenGlobalDeviceId_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new DeviceSubject
                {
                    GlobalDeviceId = 0
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((DeviceSubject)claims.Subject).GlobalDeviceId = long.MaxValue;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsync_DeviceSubject_GlobalDeviceIdMismatch_ThrowsInvalidPrivacyCommandException()
        {
            var claims = new CommandClaims
            {
                CommandId = Guid.NewGuid().ToString(),
                Operation = ValidOperation.Delete,
                Subject = new DeviceSubject
                {
                    GlobalDeviceId = long.MaxValue
                }
            };

            IList<Claim> tokenClaims = claims.GenerateClaims(MsaTestKeyId, SigningAlgo);

            // Set command info
            ((DeviceSubject)claims.Subject).GlobalDeviceId = long.MaxValue - 1;

            await this.EnsureValidAsyncEndToEnd(typeof(MsaSubject), claims, tokenClaims);
        }
        #endregion

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public async Task EnsureValidAsyncThrowsIfMalformedVerifierString()
        {
            string verifier = Guid.NewGuid().ToString();
            var validationService = new ValidationService();
            await validationService.EnsureValidAsync(verifier, new CommandClaims { Subject = new DeviceSubject() }, CancellationToken.None);
        }
    }
}
