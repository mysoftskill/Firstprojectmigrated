// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.XboxAccounts
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class XboxAccountCustomExceptionTests
    {
        #region Test Data Methods
        public static IEnumerable<object[]> GenerateExpiredXboxServiceTokenExceptionData()
        {
            var message = "this is an error message";
            const string ExpiredServiceTokenError = "An expired service token was passed in the request.";
            var innerException = new Exception();

            var data = new List<object[]>
            {
                new object[]
                {
                    new ExpiredXboxServiceTokenException(),
                    false,
                    ExpiredServiceTokenError
                },
                new object[]
                {
                    new ExpiredXboxServiceTokenException(message),
                    false,
                    message
                },
                new object[]
                {
                    new ExpiredXboxServiceTokenException(message, innerException),
                    false,
                    message
                }
            };
            return data;
        }

        public static IEnumerable<object[]> GenerateExpiredXboxUserTokenExceptionData()
        {
            var message = "this is an error message";
            const string ExpiredUserTokenError = "An expired user token was passed in the request.";
            var innerException = new Exception();

            var data = new List<object[]>
            {
                new object[]
                {
                    new ExpiredXboxUserTokenException(),
                    false,
                    ExpiredUserTokenError
                },
                new object[]
                {
                    new ExpiredXboxUserTokenException(message),
                    false,
                    message
                },
                new object[]
                {
                    new ExpiredXboxUserTokenException(message, innerException),
                    false,
                    message
                }
            };
            return data;
        }

        public static IEnumerable<object[]> GenerateInvalidXboxServiceTokenExceptionData()
        {
            var message = "this is an error message";
            const string InvalidServiceTokenError = "An invalid service token was passed in the request.";
            var innerException = new Exception();

            var data = new List<object[]>
            {
                new object[]
                {
                    new InvalidXboxServiceTokenException(),
                    false,
                    InvalidServiceTokenError
                },
                new object[]
                {
                    new InvalidXboxServiceTokenException(message),
                    false,
                    message
                },
                new object[]
                {
                    new InvalidXboxServiceTokenException(message, innerException),
                    false,
                    message
                }
            };
            return data;
        }

        public static IEnumerable<object[]> GenerateInvalidXboxUserTokenExceptionData()
        {
            var message = "this is an error message";
            const string InvalidUserTokenError = "An invalid user token was passed in the request.";
            var innerException = new Exception();

            var data = new List<object[]>
            {
                new object[]
                {
                    new InvalidXboxUserTokenException(),
                    false,
                    InvalidUserTokenError
                },
                new object[]
                {
                    new InvalidXboxUserTokenException(message),
                    false,
                    message
                },
                new object[]
                {
                    new InvalidXboxUserTokenException(message, innerException),
                    false,
                    message
                }
            };
            return data;
        }

        public static IEnumerable<object[]> GenerateXboxAccessExceptionData()
        {
            var message = "this is an error message";
            const string AccessError =
                "Access to the sandbox specified in the request was denied. You should verify that the correct sandbox "
                + "was specified in the request, and/or that the appropriate access policies were created through your DAM.";
            var innerException = new Exception();
            var data = new List<object[]>
            {
                new object[]
                {
                    new XboxAccessException(),
                    false,
                    AccessError
                },
                new object[]
                {
                    new XboxAccessException(message),
                    false,
                    message
                },
                new object[]
                {
                    new XboxAccessException(message, innerException),
                    false,
                    message
                }
            };
            return data;
        }

        public static IEnumerable<object[]> GenerateXboxAuthenticationExceptionData()
        {
            var message = "this is an error message";
            const string defaultMessage =
                @"Exception of type 'Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions.XboxAuthenticationException' was thrown.";

            var innerException = new Exception();

            var data = new List<object[]>
            {
                new object[]
                {
                    new XboxAuthenticationException(),
                    false,
                    defaultMessage
                },
                new object[]
                {
                    new XboxAuthenticationException(message),
                    false,
                    message
                },
                new object[]
                {
                    new XboxAuthenticationException(message, innerException),
                    false,
                    message
                }
            };
            return data;
        }

        public static IEnumerable<object[]> GenerateXboxOutageExceptionData()
        {
            var message = "this is an error message";
            const string XboxServiceOutageError = "Xbox Live authentication infrastructure is currently experiencing an outage.";
            var innerException = new Exception();

            var data = new List<object[]>
            {
                new object[]
                {
                    new XboxOutageException(),
                    false,
                    XboxServiceOutageError
                },
                new object[]
                {
                    new XboxOutageException(message),
                    false,
                    message
                },
                new object[]
                {
                    new XboxOutageException(message, innerException),
                    false,
                    message
                }
            };
            return data;
        }

        public static IEnumerable<object[]> GenerateXboxRequestExceptionData()
        {
            var message = "this is an error message";
            const string XboxCallError = "Xbox authentication service returned bad request exception.";
            var innerException = new Exception();

            var data = new List<object[]>
            {
                new object[]
                {
                    new XboxRequestException(),
                    false,
                    XboxCallError
                },
                new object[]
                {
                    new XboxRequestException(message),
                    false,
                    message
                },
                new object[]
                {
                    new XboxRequestException(message, innerException),
                    false,
                    message
                }
            };
            return data;
        }

        public static IEnumerable<object[]> GenerateXboxResponseExceptionData()
        {
            var message = "this is an error message";
            const string XboxCallError = "Xbox authentication service returned an invalid response.";
            var innerException = new Exception();

            var data = new List<object[]>
            {
                new object[]
                {
                    new XboxResponseException(),
                    false,
                    XboxCallError
                },
                new object[]
                {
                    new XboxResponseException(message),
                    false,
                    message
                },
                new object[]
                {
                    new XboxResponseException(message, innerException),
                    false,
                    message
                }
            };
            return data;
        }

        public static IEnumerable<object[]> GenerateXboxUserAccountExceptionData()
        {
            var message = "this is an error message";
            const string UserAccountError =
                "There is an issue with the user account. The user should be advised to resolve any issue either on the "
                + "console or by signing in to https://xbox.com or by signing in to the Xbox app on PC.";
            var innerException = new Exception();

            var data = new List<object[]>
            {
                new object[]
                {
                    new XboxUserAccountException(),
                    false,
                    UserAccountError
                },
                new object[]
                {
                    new XboxUserAccountException(123),
                    false,
                    UserAccountError
                },
                new object[]
                {
                    new XboxUserAccountException(message),
                    false,
                    message
                },
                new object[]
                {
                    new XboxUserAccountException(message, innerException),
                    false,
                    message
                }
            };
            return data;
        }
        #endregion

        #region Test for ExpiredXboxServiceTokenException

        [TestMethod]
        [ExpectedException(typeof(ExpiredXboxServiceTokenException))]
        [DynamicData(nameof(GenerateExpiredXboxServiceTokenExceptionData), DynamicDataSourceType.Method)]
        public void ThrowExpiredXboxServiceTokenExceptionException(ExpiredXboxServiceTokenException exception, bool hasInnerException, string expectedMessage)
        {
            try
            {
                throw exception;
            }
            catch (ExpiredXboxServiceTokenException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
                if (hasInnerException)
                    Assert.IsNotNull(e.InnerException);
                throw;
            }
        }

        #endregion

        #region Test for ExpiredXboxUserTokenException

        [TestMethod]
        [ExpectedException(typeof(ExpiredXboxUserTokenException))]
        [DynamicData(nameof(GenerateExpiredXboxUserTokenExceptionData), DynamicDataSourceType.Method)]
        public void ThrowExpiredXboxUserTokenExceptionException(ExpiredXboxUserTokenException exception, bool hasInnerException, string expectedMessage)
        {
            try
            {
                throw exception;
            }
            catch (ExpiredXboxUserTokenException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
                if (hasInnerException)
                    Assert.IsNotNull(e.InnerException);
                throw;
            }
        }

        #endregion

        #region Test for InvalidXboxServiceTokenException

        [TestMethod]
        [ExpectedException(typeof(InvalidXboxServiceTokenException))]
        [DynamicData(nameof(GenerateInvalidXboxServiceTokenExceptionData), DynamicDataSourceType.Method)]
        public void ThrowInvalidXboxServiceTokenException(InvalidXboxServiceTokenException exception, bool hasInnerException, string expectedMessage)
        {
            try
            {
                throw exception;
            }
            catch (InvalidXboxServiceTokenException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
                if (hasInnerException)
                    Assert.IsNotNull(e.InnerException);
                throw;
            }
        }

        #endregion

        #region Test for InvalidXboxUserTokenException

        [TestMethod]
        [ExpectedException(typeof(InvalidXboxUserTokenException))]
        [DynamicData(nameof(GenerateInvalidXboxUserTokenExceptionData), DynamicDataSourceType.Method)]
        public void ThrowInvalidXboxUserTokenException(InvalidXboxUserTokenException exception, bool hasInnerException, string expectedMessage)
        {
            try
            {
                throw exception;
            }
            catch (InvalidXboxUserTokenException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
                throw;
            }
        }

        #endregion

        #region Test for XboxAccessException

        [TestMethod]
        [ExpectedException(typeof(XboxAccessException))]
        [DynamicData(nameof(GenerateXboxAccessExceptionData), DynamicDataSourceType.Method)]
        public void ThrowXboxAccessException(XboxAccessException exception, bool hasInnerException, string expectedMessage)
        {
            try
            {
                throw exception;
            }
            catch (XboxAccessException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
                if (hasInnerException)
                    Assert.IsNotNull(e.InnerException);
                throw;
            }
        }

        #endregion

        #region Test for XboxAuthenticationException

        [TestMethod]
        [ExpectedException(typeof(XboxAuthenticationException))]
        [DynamicData(nameof(GenerateXboxAuthenticationExceptionData), DynamicDataSourceType.Method)]
        public void ThrowXboxAuthenticationException(XboxAuthenticationException exception, bool hasInnerException, string expectedMessage)
        {
            try
            {
                throw exception;
            }
            catch (XboxAuthenticationException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
                if (hasInnerException)
                    Assert.IsNotNull(e.InnerException);
                throw;
            }
        }

        #endregion

        #region Test for XboxOutageException

        [TestMethod]
        [ExpectedException(typeof(XboxOutageException))]
        [DynamicData(nameof(GenerateXboxOutageExceptionData), DynamicDataSourceType.Method)]
        public void ThrowXboxOutageException(XboxOutageException exception, bool hasInnerException, string expectedMessage)
        {
            try
            {
                throw exception;
            }
            catch (XboxOutageException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
                if (hasInnerException)
                    Assert.IsNotNull(e.InnerException);
                throw;
            }
        }

        #endregion

        #region Test for XboxRequestException

        [TestMethod]
        [ExpectedException(typeof(XboxRequestException))]
        [DynamicData(nameof(GenerateXboxRequestExceptionData), DynamicDataSourceType.Method)]
        public void ThrowXboxRequestException(XboxRequestException exception, bool hasInnerException, string expectedMessage)
        {
            try
            {
                throw exception;
            }
            catch (XboxRequestException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
                if (hasInnerException)
                    Assert.IsNotNull(e.InnerException);
                throw;
            }
        }

        #endregion

        #region Test for XboxResponseException

        [TestMethod]
        [ExpectedException(typeof(XboxResponseException))]
        [DynamicData(nameof(GenerateXboxResponseExceptionData), DynamicDataSourceType.Method)]
        public void ThrowXboxResponseException(XboxResponseException exception, bool hasInnerException, string expectedMessage)
        {
            try
            {
                throw exception;
            }
            catch (XboxResponseException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
                if (hasInnerException)
                    Assert.IsNotNull(e.InnerException);
                throw;
            }
        }

        #endregion

        #region Test for XboxUserAccountException

        [TestMethod]
        [ExpectedException(typeof(XboxUserAccountException))]
        [DynamicData(nameof(GenerateXboxUserAccountExceptionData), DynamicDataSourceType.Method)]
        public void ThrowXboxUserAccountException(XboxUserAccountException exception, bool hasInnerException, string expectedMessage)
        {
            try
            {
                throw exception;
            }
            catch (XboxUserAccountException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
                if (hasInnerException)
                    Assert.IsNotNull(e.InnerException);
                throw;
            }
        }

        #endregion
    }
}
