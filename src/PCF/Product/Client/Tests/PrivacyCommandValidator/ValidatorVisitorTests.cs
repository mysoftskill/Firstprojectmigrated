// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ValidatorVisitorTests
    {
        private readonly CommandFeedLogger logger = new ConsoleCommandFeedLogger();

        public static IEnumerable<object[]> TestInputCommandVisitorExtensionsTests =>
            new List<object[]>
            {
                // AgeOut
                new object[] { CreateCommand(new MsaSubject(), PrivacyCommandType.AgeOut), ValidOperation.AccountClose },
                new object[] { CreateCommand(new AadSubject(), PrivacyCommandType.AgeOut), ValidOperation.None },
                new object[] { CreateCommand(new DeviceSubject(), PrivacyCommandType.AgeOut), ValidOperation.None },
                new object[] { CreateCommand(new DemographicSubject(), PrivacyCommandType.AgeOut), ValidOperation.None },
                new object[] { CreateCommand(new MicrosoftEmployee(), PrivacyCommandType.AgeOut), ValidOperation.None },

                // AccountClose
                new object[] { CreateCommand(new MsaSubject(), PrivacyCommandType.AccountClose), ValidOperation.AccountClose },
                new object[] { CreateCommand(new AadSubject(), PrivacyCommandType.AccountClose), ValidOperation.AccountClose },
                new object[] { CreateCommand(new DeviceSubject(), PrivacyCommandType.AccountClose), ValidOperation.AccountClose },
                new object[] { CreateCommand(new DemographicSubject(), PrivacyCommandType.AccountClose), ValidOperation.AccountClose },
                new object[] { CreateCommand(new MicrosoftEmployee(), PrivacyCommandType.AccountClose), ValidOperation.AccountClose },
                new object[] { CreateCommand(new AadSubject2() { TenantIdType = TenantIdType.Home } , PrivacyCommandType.AccountClose), ValidOperation.AccountClose },
                new object[] { CreateCommand(new AadSubject2() { TenantIdType = TenantIdType.Resource } , PrivacyCommandType.AccountClose), ValidOperation.AccountCleanup },

                // Delete
                new object[] { CreateCommand(new MsaSubject(), PrivacyCommandType.Delete), ValidOperation.Delete },
                new object[] { CreateCommand(new AadSubject(), PrivacyCommandType.Delete), ValidOperation.Delete },
                new object[] { CreateCommand(new DeviceSubject(), PrivacyCommandType.Delete), ValidOperation.Delete },
                new object[] { CreateCommand(new DemographicSubject(), PrivacyCommandType.Delete), ValidOperation.Delete },
                new object[] { CreateCommand(new MicrosoftEmployee(), PrivacyCommandType.Delete), ValidOperation.Delete },

                // Export
                new object[] { CreateCommand(new MsaSubject(), PrivacyCommandType.Export), ValidOperation.Export },
                new object[] { CreateCommand(new AadSubject(), PrivacyCommandType.Export), ValidOperation.Export },
                new object[] { CreateCommand(new DeviceSubject(), PrivacyCommandType.Export), ValidOperation.Export },
                new object[] { CreateCommand(new DemographicSubject(), PrivacyCommandType.Export), ValidOperation.Export },
                new object[] { CreateCommand(new MicrosoftEmployee(), PrivacyCommandType.Export), ValidOperation.Export },
            };

        private static IPrivacyCommand CreateCommand(IPrivacySubject subject, PrivacyCommandType commandType)
        {
            switch (commandType)
            {
                case PrivacyCommandType.Delete:
                    return new DeleteCommand { Subject = subject };

                case PrivacyCommandType.Export:
                    return new ExportCommand { Subject = subject };

                case PrivacyCommandType.AccountClose:
                    return new AccountCloseCommand { Subject = subject };

                case PrivacyCommandType.AgeOut:
                    return new AgeOutCommand { Subject = subject };

                default:
                    throw new ArgumentOutOfRangeException(nameof(commandType), commandType, null);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(TestInputCommandVisitorExtensionsTests))]
        public void ShouldVisitCorrectly(IPrivacyCommand command, ValidOperation expectedValidOperation)
        {
            switch (command)
            {
                case AgeOutCommand ageOutCommand:
                    Assert.AreEqual(expectedValidOperation, new ValidatorVisitor(this.logger).Visit(ageOutCommand));
                    break;

                case DeleteCommand deleteCommand:
                    Assert.AreEqual(expectedValidOperation, new ValidatorVisitor(this.logger).Visit(deleteCommand));
                    break;

                case ExportCommand exportCommand:
                    Assert.AreEqual(expectedValidOperation, new ValidatorVisitor(this.logger).Visit(exportCommand));
                    break;

                case AccountCloseCommand accountCloseCommand:
                    Assert.AreEqual(expectedValidOperation, new ValidatorVisitor(this.logger).Visit(accountCloseCommand));
                    break;

                default:
                    Assert.Fail($"{command.GetType()} is not supported");
                    break;
            }
        }
    }
}
