// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    }
}
