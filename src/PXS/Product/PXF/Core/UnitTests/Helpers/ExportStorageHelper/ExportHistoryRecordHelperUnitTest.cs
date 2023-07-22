// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.ExportStorageHelper
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Unit test class for the ExportHistoryRecordHelper
    /// </summary>
    [TestClass]
    public class ExportHistoryRecordHelperUnitTest
    {
        private const int MaxHistoryRecords = 3;

        private const string requestId1 = "1706240016314794201db5176e7d";

        private const string requestId2 = "1706240016314794211db5176fff";

        private readonly DateTimeOffset completed = DateTimeOffset.UtcNow.AddSeconds(-5);

        private readonly DateTimeOffset submitted = DateTimeOffset.UtcNow.AddSeconds(-10);

        [TestMethod]
        public async Task ExportHistoryRecordHelper_CleanupEmptyList()
        {
            var existingHistory = new ExportStatusHistoryRecordCollection
            {
                HistoryRecords = new List<ExportStatusHistoryRecord>()
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Default);
            var mockSingleRecordBlob = new Mock<ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection>>(MockBehavior.Default);
            mockSingleRecordBlob
                .Setup(s => s.GetRecordAsync(true))
                .Returns<bool>(t => Task.FromResult(existingHistory));
            DateTime cutoff = DateTime.UtcNow;
            Thread.Sleep(1);
            var historyRecordHelper = new ExportHistoryRecordHelper(mockSingleRecordBlob.Object, 100, mockLogger.Object);
            Assert.IsNotNull(existingHistory);
            Assert.IsNotNull(existingHistory.HistoryRecords);
            bool deletedSomething = await historyRecordHelper.CleanupAsync(cutoff);
            Assert.AreEqual(false, deletedSomething);
            Assert.AreEqual(0, existingHistory.HistoryRecords.Count);
        }

        [TestMethod]
        public async Task ExportHistoryRecordHelper_CleanupNothing()
        {
            var existingHistory = new ExportStatusHistoryRecordCollection
            {
                HistoryRecords = new List<ExportStatusHistoryRecord>()
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Default);
            var mockSingleRecordBlob = new Mock<ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection>>(MockBehavior.Default);
            mockSingleRecordBlob
                .Setup(s => s.GetRecordAsync(true))
                .Returns<bool>(t => Task.FromResult(existingHistory));
            DateTime cutoff = DateTime.UtcNow;
            Thread.Sleep(1);
            var historyRecordHelper = new ExportHistoryRecordHelper(mockSingleRecordBlob.Object, 100, mockLogger.Object);
            for (int i = 0; i < 100; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId()
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            }
            Assert.IsNotNull(existingHistory);
            Assert.IsNotNull(existingHistory.HistoryRecords);
            string firstRequestId = existingHistory.HistoryRecords[0].ExportId;
            string lastRequestId = existingHistory.HistoryRecords[99].ExportId;
            bool deletedSomething = await historyRecordHelper.CleanupAsync(cutoff);
            Assert.AreEqual(false, deletedSomething);
            Assert.AreEqual(100, existingHistory.HistoryRecords.Count);
            Assert.AreEqual(firstRequestId, existingHistory.HistoryRecords[0].ExportId);
            Assert.AreEqual(lastRequestId, existingHistory.HistoryRecords[99].ExportId);
        }

        [TestMethod]
        public async Task ExportHistoryRecordHelper_CleanupOldRecords()
        {
            var existingHistory = new ExportStatusHistoryRecordCollection
            {
                HistoryRecords = new List<ExportStatusHistoryRecord>()
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Default);
            var mockSingleRecordBlob = new Mock<ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection>>(MockBehavior.Default);
            mockSingleRecordBlob
                .Setup(s => s.GetRecordAsync(true))
                .Returns<bool>(t => Task.FromResult(existingHistory));
            var historyRecordHelper = new ExportHistoryRecordHelper(mockSingleRecordBlob.Object, 100, mockLogger.Object);
            for (int i = 0; i < 50; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId()
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            }
            Assert.IsNotNull(existingHistory);
            Assert.IsNotNull(existingHistory.HistoryRecords);
            Assert.AreEqual(50, existingHistory.HistoryRecords.Count);
            DateTime cutoff = DateTime.UtcNow;
            Thread.Sleep(1);
            for (int i = 0; i < 50; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId()
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            }
            string firstRequestId = existingHistory.HistoryRecords[50].ExportId;
            string lastRequestId = existingHistory.HistoryRecords[99].ExportId;
            bool deletedSomething = await historyRecordHelper.CleanupAsync(cutoff);
            Assert.AreEqual(true, deletedSomething);
            Assert.AreEqual(50, existingHistory.HistoryRecords.Count);
            Assert.AreEqual(firstRequestId, existingHistory.HistoryRecords[0].ExportId);
            Assert.AreEqual(lastRequestId, existingHistory.HistoryRecords[49].ExportId);
        }

        [TestMethod]
        public async Task ExportHistoryRecordHelper_CreateFirstHistoryRecord()
        {
            var mockLogger = new Mock<ILogger>(MockBehavior.Default);
            var mockSingleRecordBlob = new Mock<ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection>>(MockBehavior.Default);
            mockSingleRecordBlob
                .Setup(s => s.GetRecordAsync(true))
                .Returns<bool>(t => Task.FromResult<ExportStatusHistoryRecordCollection>(null));
            var historyRecordHelper = new ExportHistoryRecordHelper(mockSingleRecordBlob.Object, MaxHistoryRecords, mockLogger.Object);
            var historyRecord = new ExportStatusHistoryRecord
            {
                DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                ExportId = requestId1
            };

            ExportStatusHistoryRecordCollection historyCollection = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            Assert.IsNotNull(historyCollection);
            Assert.IsNotNull(historyCollection.HistoryRecords);
            Assert.AreEqual(1, historyCollection.HistoryRecords.Count);
            Assert.AreEqual(requestId1, historyCollection.HistoryRecords[0].ExportId);
            Assert.IsNull(historyCollection.HistoryRecords[0].Completed);
            Assert.IsNull(historyCollection.HistoryRecords[0].Error);
            Assert.IsNotNull(historyCollection.HistoryRecords[0].DataTypes);
            Assert.AreEqual(1, historyCollection.HistoryRecords[0].DataTypes.Count);
            Assert.AreEqual(Policies.Current.DataTypes.Ids.PreciseUserLocation.Value, historyCollection.HistoryRecords[0].DataTypes[0]);
        }

        [TestMethod]
        public async Task ExportHistoryRecordHelper_CreateHistoryNonTestRecordsInALoop()
        {
            var existingHistory = new ExportStatusHistoryRecordCollection
            {
                HistoryRecords = new List<ExportStatusHistoryRecord>()
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Default);
            var mockSingleRecordBlob = new Mock<ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection>>(MockBehavior.Default);
            mockSingleRecordBlob
                .Setup(s => s.GetRecordAsync(true))
                .Returns<bool>(t => Task.FromResult(existingHistory));
            var historyRecordHelper = new ExportHistoryRecordHelper(mockSingleRecordBlob.Object, 3, mockLogger.Object);
            for (int i = 0; i < 3; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId()
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            }
            Assert.IsNotNull(existingHistory);
            Assert.IsNotNull(existingHistory.HistoryRecords);
            Assert.AreEqual(3, existingHistory.HistoryRecords.Count);
            for (int i = 0; i < 100; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId()
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
                Assert.IsNotNull(existingHistory);
                Assert.IsNotNull(existingHistory.HistoryRecords);
                Assert.AreEqual(3, existingHistory.HistoryRecords.Count);
                Assert.AreEqual(historyRecord.ExportId, existingHistory.HistoryRecords[2].ExportId);
            }
        }

        [TestMethod]
        public async Task ExportHistoryRecordHelper_CreateHistoryRecordPastMaxRemovingTheOldest()
        {
            var existingHistory = new ExportStatusHistoryRecordCollection
            {
                HistoryRecords = new List<ExportStatusHistoryRecord>
                {
                    new ExportStatusHistoryRecord
                    {
                        Completed = this.completed,
                        DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                        ExportId = ExportStorageProvider.GetNewRequestId(),
                        RequestedAt = this.submitted
                    },
                    new ExportStatusHistoryRecord
                    {
                        Completed = this.completed,
                        DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                        ExportId = ExportStorageProvider.GetNewRequestId(),
                        RequestedAt = this.submitted
                    },
                    new ExportStatusHistoryRecord
                    {
                        Completed = this.completed,
                        DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                        ExportId = ExportStorageProvider.GetNewRequestId(),
                        RequestedAt = this.submitted
                    }
                }
            };
            ExportStatusHistoryRecord[] saveExisting = existingHistory.HistoryRecords.ToArray();
            var mockLogger = new Mock<ILogger>(MockBehavior.Default);
            var mockSingleRecordBlob = new Mock<ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection>>(MockBehavior.Default);
            mockSingleRecordBlob
                .Setup(s => s.GetRecordAsync(true))
                .Returns<bool>(t => Task.FromResult(existingHistory));
            var historyRecordHelper = new ExportHistoryRecordHelper(mockSingleRecordBlob.Object, 3, mockLogger.Object);
            var historyRecord = new ExportStatusHistoryRecord
            {
                DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                ExportId = ExportStorageProvider.GetNewRequestId()
            };

            ExportStatusHistoryRecordCollection historyCollection = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            Assert.IsNotNull(historyCollection);
            Assert.IsNotNull(historyCollection.HistoryRecords);
            Assert.AreEqual(3, historyCollection.HistoryRecords.Count);
            Assert.AreEqual(saveExisting[1].ExportId, historyCollection.HistoryRecords[0].ExportId);
            Assert.AreEqual(saveExisting[2].ExportId, historyCollection.HistoryRecords[1].ExportId);
            Assert.AreEqual(historyRecord.ExportId, historyCollection.HistoryRecords[2].ExportId);
        }

        [TestMethod]
        public async Task ExportHistoryRecordHelper_CreateHistoryTestRecordsInALoop()
        {
            var existingHistory = new ExportStatusHistoryRecordCollection
            {
                HistoryRecords = new List<ExportStatusHistoryRecord>()
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Default);
            var mockSingleRecordBlob = new Mock<ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection>>(MockBehavior.Default);
            mockSingleRecordBlob
                .Setup(s => s.GetRecordAsync(true))
                .Returns<bool>(t => Task.FromResult(existingHistory));
            var historyRecordHelper = new ExportHistoryRecordHelper(mockSingleRecordBlob.Object, 3, mockLogger.Object);
            for (int i = 0; i < 3; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId()
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            }
            Assert.IsNotNull(existingHistory);
            Assert.IsNotNull(existingHistory.HistoryRecords);
            Assert.AreEqual(3, existingHistory.HistoryRecords.Count);
            for (int i = 0; i < 100; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId()
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
                Assert.IsNotNull(existingHistory);
                Assert.IsNotNull(existingHistory.HistoryRecords);
                Assert.AreEqual(3, existingHistory.HistoryRecords.Count);
                Assert.AreEqual(historyRecord.ExportId, existingHistory.HistoryRecords[2].ExportId);
            }
        }

        [TestMethod]
        public async Task ExportHistoryRecordHelper_CreateSecondHistoryRecord()
        {
            var existingHistory = new ExportStatusHistoryRecordCollection
            {
                HistoryRecords = new List<ExportStatusHistoryRecord>
                {
                    new ExportStatusHistoryRecord
                    {
                        Completed = this.completed,
                        DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                        ExportId = requestId1,
                        RequestedAt = this.submitted
                    }
                }
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Default);
            var mockSingleRecordBlob = new Mock<ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection>>(MockBehavior.Default);
            mockSingleRecordBlob
                .Setup(s => s.GetRecordAsync(true))
                .Returns<bool>(t => Task.FromResult(existingHistory));
            var historyRecordHelper = new ExportHistoryRecordHelper(mockSingleRecordBlob.Object, MaxHistoryRecords, mockLogger.Object);
            var historyRecord = new ExportStatusHistoryRecord
            {
                DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                ExportId = requestId2
            };

            ExportStatusHistoryRecordCollection historyCollection = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            Assert.IsNotNull(historyCollection);
            Assert.IsNotNull(historyCollection.HistoryRecords);
            Assert.AreEqual(2, historyCollection.HistoryRecords.Count);
            Assert.AreEqual(requestId1, historyCollection.HistoryRecords[0].ExportId);
            Assert.AreEqual(this.completed, historyCollection.HistoryRecords[0].Completed);
            Assert.AreEqual(this.submitted, historyCollection.HistoryRecords[0].RequestedAt);
            Assert.IsNull(historyCollection.HistoryRecords[0].Error);
            Assert.IsNotNull(historyCollection.HistoryRecords[0].DataTypes);
            Assert.AreEqual(1, historyCollection.HistoryRecords[0].DataTypes.Count);
            Assert.AreEqual(Policies.Current.DataTypes.Ids.PreciseUserLocation.Value, historyCollection.HistoryRecords[0].DataTypes[0]);
            Assert.AreEqual(requestId2, historyCollection.HistoryRecords[1].ExportId);
            Assert.IsNull(historyCollection.HistoryRecords[0].Error);
            Assert.IsNotNull(historyCollection.HistoryRecords[1].DataTypes);
            Assert.AreEqual(1, historyCollection.HistoryRecords[1].DataTypes.Count);
            Assert.AreEqual(Policies.Current.DataTypes.Ids.PreciseUserLocation.Value, historyCollection.HistoryRecords[1].DataTypes[0]);
        }

        [TestMethod]
        public async Task ExportHistoryRecordHelper_ThrottleIfCurrentRecordIsNotCompleted()
        {
            var existingHistory = new ExportStatusHistoryRecordCollection
            {
                HistoryRecords = new List<ExportStatusHistoryRecord>()
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Default);
            var mockSingleRecordBlob = new Mock<ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection>>(MockBehavior.Default);
            mockSingleRecordBlob
                .Setup(s => s.GetRecordAsync(true))
                .Returns<bool>(t => Task.FromResult(existingHistory));
            var historyRecordHelper = new ExportHistoryRecordHelper(mockSingleRecordBlob.Object, 100, mockLogger.Object);
            for (int i = 0; i < 50; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId(),
                    RequestedAt = DateTimeOffset.UtcNow,
                    Completed = DateTimeOffset.UtcNow
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            }
            Assert.IsNotNull(existingHistory);
            Assert.IsNotNull(existingHistory.HistoryRecords);
            Assert.AreEqual(50, existingHistory.HistoryRecords.Count);
            DateTime cutoff = DateTime.UtcNow;
            Thread.Sleep(1);
            for (int i = 0; i < 48; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId(),
                    RequestedAt = DateTimeOffset.UtcNow,
                    Completed = DateTimeOffset.UtcNow
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            }
            var lastRecord = new ExportStatusHistoryRecord
            {
                DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                ExportId = ExportStorageProvider.GetNewRequestId(),
                RequestedAt = DateTimeOffset.UtcNow
            };

            existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(lastRecord);

            var incomingRecord = new ExportStatusHistoryRecord
            {
                DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                ExportId = ExportStorageProvider.GetNewRequestId(),
                RequestedAt = DateTimeOffset.UtcNow
            };

            ExportThrottleState throttleState = await historyRecordHelper.CheckRequestThrottlingAsync(incomingRecord, cutoff, 50, 100);
            Assert.AreEqual(ExportThrottleState.RequestInProgress, throttleState);
        }

        [TestMethod]
        public async Task ExportHistoryRecordHelper_ThrottleOnCancelledOrErroredRecords()
        {
            var existingHistory = new ExportStatusHistoryRecordCollection
            {
                HistoryRecords = new List<ExportStatusHistoryRecord>()
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Default);
            var mockSingleRecordBlob = new Mock<ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection>>(MockBehavior.Default);
            mockSingleRecordBlob
                .Setup(s => s.GetRecordAsync(true))
                .Returns<bool>(t => Task.FromResult(existingHistory));
            var historyRecordHelper = new ExportHistoryRecordHelper(mockSingleRecordBlob.Object, 100, mockLogger.Object);
            for (int i = 0; i < 50; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId(),
                    RequestedAt = DateTimeOffset.UtcNow,
                    Completed = DateTimeOffset.UtcNow
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            }
            Assert.IsNotNull(existingHistory);
            Assert.IsNotNull(existingHistory.HistoryRecords);
            Assert.AreEqual(50, existingHistory.HistoryRecords.Count);
            DateTime cutoff = DateTime.UtcNow;
            Thread.Sleep(1);
            for (int i = 0; i < 50; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId(),
                    RequestedAt = DateTimeOffset.UtcNow,
                    Error = "badrequest",
                    Completed = DateTimeOffset.UtcNow
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            }
            var incomingRecord = new ExportStatusHistoryRecord
            {
                DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                ExportId = ExportStorageProvider.GetNewRequestId(),
                RequestedAt = DateTimeOffset.UtcNow
            };

            ExportThrottleState throttleState = await historyRecordHelper.CheckRequestThrottlingAsync(incomingRecord, cutoff, 100, 50);
            Assert.AreEqual(ExportThrottleState.TooManyRequests, throttleState);
        }

        [TestMethod]
        public async Task ExportHistoryRecordHelper_ThrottleOnCompletedRecords()
        {
            var existingHistory = new ExportStatusHistoryRecordCollection
            {
                HistoryRecords = new List<ExportStatusHistoryRecord>()
            };
            var mockLogger = new Mock<ILogger>(MockBehavior.Default);
            var mockSingleRecordBlob = new Mock<ISingleRecordBlobHelper<ExportStatusHistoryRecordCollection>>(MockBehavior.Default);
            mockSingleRecordBlob
                .Setup(s => s.GetRecordAsync(true))
                .Returns<bool>(t => Task.FromResult(existingHistory));
            var historyRecordHelper = new ExportHistoryRecordHelper(mockSingleRecordBlob.Object, 100, mockLogger.Object);
            for (int i = 0; i < 50; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId(),
                    RequestedAt = DateTimeOffset.UtcNow,
                    Completed = DateTimeOffset.UtcNow
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            }
            Assert.IsNotNull(existingHistory);
            Assert.IsNotNull(existingHistory.HistoryRecords);
            Assert.AreEqual(50, existingHistory.HistoryRecords.Count);
            DateTime cutoff = DateTime.UtcNow;
            Thread.Sleep(1);
            for (int i = 0; i < 50; i++)
            {
                var historyRecord = new ExportStatusHistoryRecord
                {
                    DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                    ExportId = ExportStorageProvider.GetNewRequestId(),
                    RequestedAt = DateTimeOffset.UtcNow,
                    Completed = DateTimeOffset.UtcNow
                };

                existingHistory = await historyRecordHelper.CreateHistoryRecordAsync(historyRecord);
            }
            var incomingRecord = new ExportStatusHistoryRecord
            {
                DataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value },
                ExportId = ExportStorageProvider.GetNewRequestId(),
                RequestedAt = DateTimeOffset.UtcNow
            };

            ExportThrottleState throttleState = await historyRecordHelper.CheckRequestThrottlingAsync(incomingRecord, cutoff, 50, 100);
            Assert.AreEqual(ExportThrottleState.TooManyRequests, throttleState);
        }
    }
}
