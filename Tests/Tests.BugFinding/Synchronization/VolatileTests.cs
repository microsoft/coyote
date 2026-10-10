// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;
using Volatile = System.Threading.Volatile;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class VolatileTests : BaseBugFindingTest
    {
        public VolatileTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestVolatileReadLong()
        {
            this.Test(() =>
            {
                long value = long.MaxValue - 42;
                Assert.Equal(long.MaxValue - 42, Volatile.Read(ref value));
            }, configuration: this.GetConfiguration().WithVolatileOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestVolatileReadULong()
        {
            this.Test(() =>
            {
                ulong value = ulong.MaxValue - 42;
                Assert.Equal(ulong.MaxValue - 42, Volatile.Read(ref value));
            }, configuration: this.GetConfiguration().WithVolatileOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestVolatileWriteLong()
        {
            this.Test(() =>
            {
                long value = long.MaxValue;
                Volatile.Write(ref value, long.MaxValue - 42);
                Assert.Equal(long.MaxValue - 42, value);
            }, configuration: this.GetConfiguration().WithVolatileOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestVolatileWriteULong()
        {
            this.Test(() =>
            {
                ulong value = ulong.MaxValue;
                Volatile.Write(ref value, ulong.MaxValue - 42);
                Assert.Equal(ulong.MaxValue - 42, value);
            }, configuration: this.GetConfiguration().WithVolatileOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestVolatileAccessesAreSchedulingPoints()
        {
            // Each task reads and then writes the value without retrying, so an update is lost only
            // if the scheduler can interleave the two volatile accesses.
            this.TestWithError(() =>
            {
                int value = 0;
                void IncrementOnce() => Volatile.Write(ref value, Volatile.Read(ref value) + 1);
                Task first = Task.Run(IncrementOnce);
                Task second = Task.Run(IncrementOnce);
                Task.WaitAll(first, second);
                Specification.Assert(value is 2, "Lost an update.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100)
                .WithVolatileOperationRaceCheckingEnabled(true),
            expectedError: "Lost an update.",
            replay: true);
        }

#if NET10_0_OR_GREATER
        [Fact(Timeout = 5000)]
        public void TestMemoryBarriersAreNotSchedulingPoints()
        {
            // The barriers are invoked natively without a scheduling point and plain field accesses are
            // not scheduling points either, so the read and write of each task cannot be interleaved.
            this.Test(() =>
            {
                int value = 0;
                void IncrementOnce()
                {
                    Volatile.ReadBarrier();
                    int old = value;
                    Volatile.WriteBarrier();
                    value = old + 1;
                }

                Task first = Task.Run(IncrementOnce);
                Task second = Task.Run(IncrementOnce);
                Task.WaitAll(first, second);
                Specification.Assert(value is 2, "Lost an update.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100)
                .WithVolatileOperationRaceCheckingEnabled(true)
                .WithMemoryAccessRaceCheckingEnabled(false));
        }
#endif
    }
}
