using System;
using CacheXt.Core;
using CacheXt.NCache;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class NCacheWrapperTests
    {
        private static readonly ICacheWrapper subject;
        const string key1 = "key";
        private readonly TimeSpan _timeSpan = new TimeSpan(0, 1, 0);

        static NCacheWrapperTests()
        {
            subject = new NCacheWrapper(new NCacheConfig());
        }

        [TestMethod]
        public void GivenKeyNotInCache_WhenUpdateWithLockInParallel_ThenNoDataLostPerIteration()
        {
            // Arrange
            subject.Remove(key1);

            // Act
            DateTime started = DateTime.UtcNow;
            PerformParallelTest(key1, _timeSpan);

            // Assert
            AssertSuccess(key1, started, _timeSpan);
        }

        [TestMethod]
        public void GivenKeyNotInCache_WhenUpdateWithLockSerially_ThenNoDataLostPerIteration()
        {
            // Arrange
            subject.Remove(key1);

            // Act
            DateTime started = DateTime.UtcNow;
            PerformSerialTest(key1, _timeSpan);

            // Assert
            AssertSuccess(key1, started, _timeSpan);
        }

        [TestMethod]
        public void GivenKeyInCache_WhenUpdateWithLockInParallel_ThenNoDataLostPerIteration()
        {
            // Arrange
            subject.Remove(key1);
            subject.Set(key1, string.Empty);

            // Act
            DateTime started = DateTime.UtcNow;
            PerformParallelTest(key1, _timeSpan);

            // Assert
            AssertSuccess(key1, started, _timeSpan);
        }

        [TestMethod]
        public void GivenKeyInCache_WhenUpdateWithLockSerially_ThenNoDataLostPerIteration()
        {
            // Arrange
            subject.Remove(key1);
            subject.Set(key1, string.Empty);

            // Act
            DateTime started = DateTime.UtcNow;
            PerformSerialTest(key1, _timeSpan);

            // Assert
            AssertSuccess(key1, started, _timeSpan);
        }

        [TestMethod]
        public void GivenKeyInCache_WhenUpdateWithLockSerially_AndUpdateThrows_ThenLockReleased()
        {
            // Arrange
            var key2 = key1 + "x";
            const string expectedFailReason = "I failed...falling on my sword now...";
            subject.Remove(key2);
            subject.Set(key2, string.Empty);

            // Act
            DateTime started = DateTime.UtcNow;
            for (int i = 0; i < 16; ++i)
            {
                try
                {
                    subject.UpdateWithLock(
                        key2,
                        () => string.Empty,
                        x => { throw new InvalidOperationException(expectedFailReason); },
                        _timeSpan);
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message != expectedFailReason)
                    {
                        throw;
                    }
                    // now do it correct way...
                    TestOperation(key2, i, _timeSpan);
                }
            }

            // Assert
            AssertSuccess(key2, started, _timeSpan);
        }

        private static void AssertSuccess(string key1, DateTime started, TimeSpan timeSpan)
        {
            var result = (string)subject.Get(key1);
            var results = result.Split(';');
            Assert.AreEqual(16, results.Length, "Some values were skipped");
            Assert.IsTrue(DateTime.UtcNow.Subtract(started) <= timeSpan, "Took too long");
        }

        private static void PerformParallelTest(string key1, TimeSpan timeSpan)
        {
            Enumerable.Range(1, 16)
                .AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .AsUnordered()
                .WithDegreeOfParallelism(16)
                .ForAll(n => TestOperation(key1, n, timeSpan)
                );
        }

        private static void PerformSerialTest(string key1, TimeSpan timeSpan)
        {
            for(int n = 1; n <= 16; ++n)
            {
                TestOperation(key1, n, timeSpan);
            }
        }

        private static void TestOperation(string key1, int n, TimeSpan timeSpan)
        {
            subject.UpdateWithLock(
                key1,
                () => string.Empty,
                x => string.IsNullOrWhiteSpace(x) ? n.ToString() : string.Format("{0};{1}", x, n),
                timeSpan);
        }
    }
}
