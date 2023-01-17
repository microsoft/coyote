// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Interlocked = System.Threading.Interlocked;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class InterlockedTests : BaseBugFindingTest
    {
        public InterlockedTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedReadLong()
        {
            this.Test(() =>
            {
                long value = long.MaxValue - 42;
                Assert.Equal(long.MaxValue - 42, Interlocked.Read(ref value));
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 5000)]
        public void TestInterlockedReadULong()
        {
            this.Test(() =>
            {
                ulong value = ulong.MaxValue - 42;
                Assert.Equal(ulong.MaxValue - 42, Interlocked.Read(ref value));
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestInterlockedAddInt()
        {
            this.Test(() =>
            {
                int value = 42;
                Assert.Equal(12387, Interlocked.Add(ref value, 12345));
                Assert.Equal(12387, value);
                Assert.Equal(12387, Interlocked.Add(ref value, 0));
                Assert.Equal(12387, value);
                Assert.Equal(12386, Interlocked.Add(ref value, -1));
                Assert.Equal(12386, value);

                value = int.MaxValue;
                Assert.Equal(int.MinValue, Interlocked.Add(ref value, 1));
                Assert.Equal(int.MinValue, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedAddLong()
        {
            this.Test(() =>
            {
                long value = 42;
                Assert.Equal(12387, Interlocked.Add(ref value, 12345));
                Assert.Equal(12387, value);
                Assert.Equal(12387, Interlocked.Add(ref value, 0));
                Assert.Equal(12387, value);
                Assert.Equal(12386, Interlocked.Add(ref value, -1));
                Assert.Equal(12386, value);

                value = long.MaxValue;
                Assert.Equal(long.MinValue, Interlocked.Add(ref value, 1));
                Assert.Equal(long.MinValue, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 5000)]
        public void TestInterlockedAddUInt()
        {
            this.Test(() =>
            {
                uint value = 42;
                Assert.Equal(12387u, Interlocked.Add(ref value, 12345u));
                Assert.Equal(12387u, value);
                Assert.Equal(12387u, Interlocked.Add(ref value, 0u));
                Assert.Equal(12387u, value);
                Assert.Equal(9386u, Interlocked.Add(ref value, 4294964295u));
                Assert.Equal(9386u, value);

                value = uint.MaxValue;
                Assert.Equal(0u, Interlocked.Add(ref value, 1));
                Assert.Equal(0u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedAddULong()
        {
            this.Test(() =>
            {
                ulong value = 42;
                Assert.Equal(12387u, Interlocked.Add(ref value, 12345));
                Assert.Equal(12387u, value);
                Assert.Equal(12387u, Interlocked.Add(ref value, 0));
                Assert.Equal(12387u, value);
                Assert.Equal(10771u, Interlocked.Add(ref value, 18446744073709550000));
                Assert.Equal(10771u, value);

                value = ulong.MaxValue;
                Assert.Equal(0u, Interlocked.Add(ref value, 1));
                Assert.Equal(0u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestInterlockedIncrementInt()
        {
            this.Test(() =>
            {
                int value = 42;
                Assert.Equal(43, Interlocked.Increment(ref value));
                Assert.Equal(43, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedIncrementLong()
        {
            this.Test(() =>
            {
                long value = 42;
                Assert.Equal(43, Interlocked.Increment(ref value));
                Assert.Equal(43, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 5000)]
        public void TestInterlockedIncrementUInt()
        {
            this.Test(() =>
            {
                uint value = 42u;
                Assert.Equal(43u, Interlocked.Increment(ref value));
                Assert.Equal(43u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedIncrementULong()
        {
            this.Test(() =>
            {
                ulong value = 42u;
                Assert.Equal(43u, Interlocked.Increment(ref value));
                Assert.Equal(43u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestInterlockedDecrementInt()
        {
            this.Test(() =>
            {
                int value = 42;
                Assert.Equal(41, Interlocked.Decrement(ref value));
                Assert.Equal(41, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedDecrementLong()
        {
            this.Test(() =>
            {
                long value = 42;
                Assert.Equal(41, Interlocked.Decrement(ref value));
                Assert.Equal(41, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 5000)]
        public void TestInterlockedDecrementUInt()
        {
            this.Test(() =>
            {
                uint value = 42u;
                Assert.Equal(41u, Interlocked.Decrement(ref value));
                Assert.Equal(41u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedDecrementULong()
        {
            this.Test(() =>
            {
                ulong value = 42u;
                Assert.Equal(41u, Interlocked.Decrement(ref value));
                Assert.Equal(41u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestInterlockedExchangeInt()
        {
            this.Test(() =>
            {
                int value = 42;
                Assert.Equal(42, Interlocked.Exchange(ref value, 12345));
                Assert.Equal(12345, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedExchangeLong()
        {
            this.Test(() =>
            {
                long value = 42;
                Assert.Equal(42, Interlocked.Exchange(ref value, 12345));
                Assert.Equal(12345, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 5000)]
        public void TestInterlockedExchangeUInt()
        {
            this.Test(() =>
            {
                uint value = 42;
                Assert.Equal(42u, Interlocked.Exchange(ref value, 12345u));
                Assert.Equal(12345u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedExchangeULong()
        {
            this.Test(() =>
            {
                ulong value = 42;
                Assert.Equal(42u, Interlocked.Exchange(ref value, 12345u));
                Assert.Equal(12345u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestInterlockedExchangeFloat()
        {
            this.Test(() =>
            {
                float value = 42.1f;
                Assert.Equal(42.1f, Interlocked.Exchange(ref value, 12345.1f));
                Assert.Equal(12345.1f, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedExchangeDouble()
        {
            this.Test(() =>
            {
                double value = 42.1;
                Assert.Equal(42.1, Interlocked.Exchange(ref value, 12345.1));
                Assert.Equal(12345.1, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedExchangeObject()
        {
            this.Test(() =>
            {
                var oldValue = new object();
                var newValue = new object();
                object value = oldValue;

                Assert.Same(oldValue, Interlocked.Exchange(ref value, newValue));
                Assert.Same(newValue, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedExchangeBoxedObject()
        {
            this.Test(() =>
            {
                var oldValue = (object)42;
                var newValue = (object)12345;
                object value = oldValue;

                object valueBeforeUpdate = Interlocked.Exchange(ref value, newValue);
                Assert.Same(oldValue, valueBeforeUpdate);
                Assert.Equal(42, (int)valueBeforeUpdate);
                Assert.Same(newValue, value);
                Assert.Equal(12345, (int)value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedCompareExchangeInt()
        {
            this.Test(() =>
            {
                int value = 42;

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 41));
                Assert.Equal(42, value);

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 42));
                Assert.Equal(12345, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedCompareExchangeLong()
        {
            this.Test(() =>
            {
                long value = 42;

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 41));
                Assert.Equal(42, value);

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 42));
                Assert.Equal(12345, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 5000)]
        public void TestInterlockedCompareExchangeUInt()
        {
            this.Test(() =>
            {
                uint value = 42;

                Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 41u));
                Assert.Equal(42u, value);

                Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 42u));
                Assert.Equal(12345u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedCompareExchangeULong()
        {
            this.Test(() =>
            {
                ulong value = 42;

                Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 41u));
                Assert.Equal(42u, value);

                Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 42u));
                Assert.Equal(12345u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestInterlockedCompareExchangeFloat()
        {
            this.Test(() =>
            {
                float value = 42.1f;

                Assert.Equal(42.1f, Interlocked.CompareExchange(ref value, 12345.1f, 41.1f));
                Assert.Equal(42.1f, value);

                Assert.Equal(42.1f, Interlocked.CompareExchange(ref value, 12345.1f, 42.1f));
                Assert.Equal(12345.1f, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedCompareExchangeDouble()
        {
            this.Test(() =>
            {
                double value = 42.1;

                Assert.Equal(42.1, Interlocked.CompareExchange(ref value, 12345.1, 41.1));
                Assert.Equal(42.1, value);

                Assert.Equal(42.1, Interlocked.CompareExchange(ref value, 12345.1, 42.1));
                Assert.Equal(12345.1, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedCompareExchangeObject()
        {
            this.Test(() =>
            {
                var oldValue = new object();
                var newValue = new object();
                object value = oldValue;

                Assert.Same(oldValue, Interlocked.CompareExchange(ref value, newValue, new object()));
                Assert.Same(oldValue, value);

                Assert.Same(oldValue, Interlocked.CompareExchange(ref value, newValue, oldValue));
                Assert.Same(newValue, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedCompareExchangeBoxedObject()
        {
            this.Test(() =>
            {
                var oldValue = (object)42;
                var newValue = (object)12345;
                object value = oldValue;

                object valueBeforeUpdate = Interlocked.CompareExchange(ref value, newValue, (object)42);
                Assert.Same(oldValue, valueBeforeUpdate);
                Assert.Equal(42, (int)valueBeforeUpdate);
                Assert.Same(oldValue, value);
                Assert.Equal(42, (int)value);

                valueBeforeUpdate = Interlocked.CompareExchange(ref value, newValue, oldValue);
                Assert.Same(oldValue, valueBeforeUpdate);
                Assert.Equal(42, (int)valueBeforeUpdate);
                Assert.Same(newValue, value);
                Assert.Equal(12345, (int)value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 5000)]
        public void TestInterlockedAndInt()
        {
            this.Test(() =>
            {
                int value = 0x12345670;
                Assert.Equal(0x12345670, Interlocked.And(ref value, 0x7654321));
                Assert.Equal(0x02244220, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedAndLong()
        {
            this.Test(() =>
            {
                long value = 0x12345670;
                Assert.Equal(0x12345670, Interlocked.And(ref value, 0x7654321));
                Assert.Equal(0x02244220, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedAndUInt()
        {
            this.Test(() =>
            {
                uint value = 0x12345670u;
                Assert.Equal(0x12345670u, Interlocked.And(ref value, 0x7654321));
                Assert.Equal(0x02244220u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedAndULong()
        {
            this.Test(() =>
            {
                ulong value = 0x12345670u;
                Assert.Equal(0x12345670u, Interlocked.And(ref value, 0x7654321));
                Assert.Equal(0x02244220u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedOrInt()
        {
            this.Test(() =>
            {
                int value = 0x12345670;
                Assert.Equal(0x12345670, Interlocked.Or(ref value, 0x7654321));
                Assert.Equal(0x17755771, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedOrLong()
        {
            this.Test(() =>
            {
                long value = 0x12345670;
                Assert.Equal(0x12345670, Interlocked.Or(ref value, 0x7654321));
                Assert.Equal(0x17755771, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedOrUInt()
        {
            this.Test(() =>
            {
                uint value = 0x12345670u;
                Assert.Equal(0x12345670u, Interlocked.Or(ref value, 0x7654321));
                Assert.Equal(0x17755771u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedOrULong()
        {
            this.Test(() =>
            {
                ulong value = 0x12345670u;
                Assert.Equal(0x12345670u, Interlocked.Or(ref value, 0x7654321));
                Assert.Equal(0x17755771u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestInterlockedConcurrentIncrementInt()
        {
            this.Test(() =>
            {
                int value = 0;
                const int taskCount = 3;
                const int iterationCount = 10;
                var tasks = new Task[taskCount];
                for (int i = 0; i < taskCount; ++i)
                {
                    tasks[i] = Task.Run(() =>
                    {
                        for (int i = 0; i < iterationCount; ++i)
                        {
                            Interlocked.Increment(ref value);
                        }
                    });
                }

                Task.WaitAll(tasks);
                Assert.Equal(taskCount * iterationCount, value);
            }, configuration: this.GetConfiguration().WithTestingIterations(10)
                .WithAtomicOperationRaceCheckingEnabled(true));
        }
    }
}
