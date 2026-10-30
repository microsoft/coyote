// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
#if NET9_0_OR_GREATER
using Microsoft.Coyote.Tests.Common.Threading;
#endif
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

#if NET9_0_OR_GREATER
        [Theory(Timeout = 5000)]
        [InlineData(nameof(Byte))]
        [InlineData(nameof(SByte))]
        [InlineData(nameof(Int16))]
        [InlineData(nameof(UInt16))]
        public void TestInterlockedSmallIntegerExchangeAndCompareExchange(string typeName)
        {
            this.Test(() =>
            {
                switch (typeName)
                {
                    case nameof(Byte):
                        byte b = 42;
                        Assert.Equal((byte)42, Interlocked.Exchange(ref b, (byte)255));
                        Assert.Equal((byte)255, b);
                        Assert.Equal((byte)255, Interlocked.CompareExchange(ref b, (byte)7, (byte)41));
                        Assert.Equal((byte)255, b);
                        Assert.Equal((byte)255, Interlocked.CompareExchange(ref b, (byte)7, (byte)255));
                        Assert.Equal((byte)7, b);
                        break;
                    case nameof(SByte):
                        sbyte sb = 42;
                        Assert.Equal((sbyte)42, Interlocked.Exchange(ref sb, (sbyte)-128));
                        Assert.Equal((sbyte)-128, sb);
                        Assert.Equal((sbyte)-128, Interlocked.CompareExchange(ref sb, (sbyte)7, (sbyte)-127));
                        Assert.Equal((sbyte)-128, sb);
                        Assert.Equal((sbyte)-128, Interlocked.CompareExchange(ref sb, (sbyte)7, (sbyte)-128));
                        Assert.Equal((sbyte)7, sb);
                        break;
                    case nameof(Int16):
                        short s = 42;
                        Assert.Equal((short)42, Interlocked.Exchange(ref s, short.MinValue));
                        Assert.Equal(short.MinValue, s);
                        Assert.Equal(short.MinValue, Interlocked.CompareExchange(ref s, (short)7, short.MaxValue));
                        Assert.Equal(short.MinValue, s);
                        Assert.Equal(short.MinValue, Interlocked.CompareExchange(ref s, (short)7, short.MinValue));
                        Assert.Equal((short)7, s);
                        break;
                    default:
                        ushort us = 42;
                        Assert.Equal((ushort)42, Interlocked.Exchange(ref us, ushort.MaxValue));
                        Assert.Equal(ushort.MaxValue, us);
                        Assert.Equal(ushort.MaxValue, Interlocked.CompareExchange(ref us, (ushort)7, (ushort)41));
                        Assert.Equal(ushort.MaxValue, us);
                        Assert.Equal(ushort.MaxValue, Interlocked.CompareExchange(ref us, (ushort)7, ushort.MaxValue));
                        Assert.Equal((ushort)7, us);
                        break;
                }
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Theory(Timeout = 5000)]
        [InlineData(nameof(Byte))]
        [InlineData(nameof(SByte))]
        [InlineData(nameof(Int16))]
        [InlineData(nameof(UInt16))]
        public void TestInterlockedSmallIntegerLostUpdateIsFound(string typeName)
        {
            // Each task reads the value atomically and then writes the incremented value atomically
            // without retrying, so an update is lost only if the scheduler interleaves the two
            // atomic operations, which requires the operations to be controlled scheduling points.
            this.TestWithError(() =>
            {
                byte b = 0;
                sbyte sb = 0;
                short s = 0;
                ushort us = 0;
                void IncrementOnce()
                {
                    switch (typeName)
                    {
                        case nameof(Byte):
                            byte oldByte = Interlocked.CompareExchange(ref b, 0, 0);
                            Interlocked.Exchange(ref b, (byte)(oldByte + 1));
                            break;
                        case nameof(SByte):
                            sbyte oldSByte = Interlocked.CompareExchange(ref sb, 0, 0);
                            Interlocked.Exchange(ref sb, (sbyte)(oldSByte + 1));
                            break;
                        case nameof(Int16):
                            short oldShort = Interlocked.CompareExchange(ref s, 0, 0);
                            Interlocked.Exchange(ref s, (short)(oldShort + 1));
                            break;
                        default:
                            ushort oldUShort = Interlocked.CompareExchange(ref us, 0, 0);
                            Interlocked.Exchange(ref us, (ushort)(oldUShort + 1));
                            break;
                    }
                }

                Task first = Task.Run(IncrementOnce);
                Task second = Task.Run(IncrementOnce);
                Task.WaitAll(first, second);
                int total = b + sb + s + us;
                Specification.Assert(total is 2, "Lost an update.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100)
                .WithAtomicOperationRaceCheckingEnabled(true),
            expectedError: "Lost an update.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedExplicitGenericExchangeAndCompareExchange()
        {
            var oldReference = new object();
            var newReference = new object();
            string[] expected =
            {
                InterlockedProvider.GetExchangeOutcome<object>(oldReference, newReference),
                InterlockedProvider.GetCompareExchangeOutcome<object>(oldReference, newReference, new object()),
                InterlockedProvider.GetCompareExchangeOutcome<object>(oldReference, newReference, oldReference),
                InterlockedProvider.GetExchangeOutcome<int>(42, -1),
                InterlockedProvider.GetCompareExchangeOutcome<int>(42, -1, 41),
                InterlockedProvider.GetCompareExchangeOutcome<int>(42, -1, 42),
                InterlockedProvider.GetExchangeOutcome<ByteEnum>(ByteEnum.First, ByteEnum.Last),
                InterlockedProvider.GetCompareExchangeOutcome<ByteEnum>(ByteEnum.First, ByteEnum.Last, ByteEnum.Last),
                InterlockedProvider.GetCompareExchangeOutcome<ByteEnum>(ByteEnum.First, ByteEnum.Last, ByteEnum.First),
                InterlockedProvider.GetExchangeOutcome<LongEnum>(LongEnum.First, LongEnum.Last),
                InterlockedProvider.GetCompareExchangeOutcome<LongEnum>(LongEnum.First, LongEnum.Last, LongEnum.Last),
                InterlockedProvider.GetCompareExchangeOutcome<LongEnum>(LongEnum.First, LongEnum.Last, LongEnum.First)
            };

            this.Test(() =>
            {
                object reference = oldReference;
                object comparedReference = oldReference;
                object matchedReference = oldReference;
                int primitive = 42;
                int comparedPrimitive = 42;
                int matchedPrimitive = 42;
                ByteEnum byteEnum = ByteEnum.First;
                ByteEnum comparedByteEnum = ByteEnum.First;
                ByteEnum matchedByteEnum = ByteEnum.First;
                LongEnum longEnum = LongEnum.First;
                LongEnum comparedLongEnum = LongEnum.First;
                LongEnum matchedLongEnum = LongEnum.First;
                string[] actual =
                {
                    InterlockedProvider.GetOutcome(
                        () => Interlocked.Exchange<object>(ref reference, newReference), () => reference),
                    InterlockedProvider.GetOutcome(
                        () => Interlocked.CompareExchange<object>(ref comparedReference, newReference, new object()),
                        () => comparedReference),
                    InterlockedProvider.GetOutcome(
                        () => Interlocked.CompareExchange<object>(ref matchedReference, newReference, oldReference),
                        () => matchedReference),
                    InterlockedProvider.GetOutcome(
                        () => Interlocked.Exchange<int>(ref primitive, -1), () => primitive),
                    InterlockedProvider.GetOutcome(
                        () => Interlocked.CompareExchange<int>(ref comparedPrimitive, -1, 41), () => comparedPrimitive),
                    InterlockedProvider.GetOutcome(
                        () => Interlocked.CompareExchange<int>(ref matchedPrimitive, -1, 42), () => matchedPrimitive),
                    InterlockedProvider.GetOutcome(
                        () => Interlocked.Exchange<ByteEnum>(ref byteEnum, ByteEnum.Last), () => byteEnum),
                    InterlockedProvider.GetOutcome(
                        () => Interlocked.CompareExchange<ByteEnum>(ref comparedByteEnum, ByteEnum.Last, ByteEnum.Last),
                        () => comparedByteEnum),
                    InterlockedProvider.GetOutcome(
                        () => Interlocked.CompareExchange<ByteEnum>(ref matchedByteEnum, ByteEnum.Last, ByteEnum.First),
                        () => matchedByteEnum),
                    InterlockedProvider.GetOutcome(
                        () => Interlocked.Exchange<LongEnum>(ref longEnum, LongEnum.Last), () => longEnum),
                    InterlockedProvider.GetOutcome(
                        () => Interlocked.CompareExchange<LongEnum>(ref comparedLongEnum, LongEnum.Last, LongEnum.Last),
                        () => comparedLongEnum),
                    InterlockedProvider.GetOutcome(
                        () => Interlocked.CompareExchange<LongEnum>(ref matchedLongEnum, LongEnum.Last, LongEnum.First),
                        () => matchedLongEnum)
                };

                for (int idx = 0; idx < expected.Length; idx++)
                {
                    Specification.Assert(actual[idx] == expected[idx],
                        "Found outcome '{0}' instead of the uncontrolled outcome '{1}'.", actual[idx], expected[idx]);
                }
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedExplicitGenericWithUnsupportedValueType()
        {
            var oldValue = new DateTime(2000, 1, 1);
            var newValue = new DateTime(2001, 1, 1);
            string expectedExchange = InterlockedProvider.GetExchangeOutcome<DateTime>(oldValue, newValue);
            string expectedCompareExchange = InterlockedProvider.GetCompareExchangeOutcome<DateTime>(
                oldValue, newValue, oldValue);
            Assert.StartsWith(typeof(NotSupportedException).FullName, expectedExchange);
            Assert.StartsWith(typeof(NotSupportedException).FullName, expectedCompareExchange);

            this.Test(() =>
            {
                DateTime value = oldValue;
                string actualExchange = InterlockedProvider.GetOutcome(
                    () => Interlocked.Exchange<DateTime>(ref value, newValue), () => value);
                string actualCompareExchange = InterlockedProvider.GetOutcome(
                    () => Interlocked.CompareExchange<DateTime>(ref value, newValue, oldValue), () => value);
                Specification.Assert(actualExchange == expectedExchange,
                    "Found outcome '{0}' instead of the uncontrolled outcome '{1}'.", actualExchange, expectedExchange);
                Specification.Assert(actualCompareExchange == expectedCompareExchange,
                    "Found outcome '{0}' instead of the uncontrolled outcome '{1}'.",
                    actualCompareExchange, expectedCompareExchange);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedExplicitGenericLostUpdateIsFound()
        {
            this.TestWithError(() =>
            {
                LongEnum value = LongEnum.First;
                void IncrementOnce()
                {
                    LongEnum old = Interlocked.CompareExchange<LongEnum>(ref value, LongEnum.First, LongEnum.First);
                    Interlocked.Exchange<LongEnum>(ref value, old + 1);
                }

                Task first = Task.Run(IncrementOnce);
                Task second = Task.Run(IncrementOnce);
                Task.WaitAll(first, second);
                Specification.Assert(value == LongEnum.First + 2, "Lost an update.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100)
                .WithAtomicOperationRaceCheckingEnabled(true),
            expectedError: "Lost an update.",
            replay: true);
        }

        private enum ByteEnum : byte
        {
            First = 1,
            Last = byte.MaxValue
        }

        private enum LongEnum : long
        {
            First = long.MinValue,
            Last = long.MaxValue
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
