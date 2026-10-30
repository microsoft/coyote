// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Mono.Reflection;
using Xunit;
using Xunit.Abstractions;
using CoyoteInterlocked = Microsoft.Coyote.Rewriting.Types.Threading.Interlocked;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class InterlockedRewritingTests : BaseRewritingTest
    {
        public InterlockedRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private enum ShortEnum : short
        {
            First = -1,
            Last = short.MaxValue
        }

        private enum ULongEnum : ulong
        {
            First = 1,
            Last = ulong.MaxValue
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingSmallIntegerAtomicOverloads()
        {
            IReadOnlyList<MethodInfo> calls = GetAtomicCalls(nameof(InvokeSmallIntegerOverloads));
            Assert.Equal(8, calls.Count);
            Assert.All(calls, call => Assert.Equal(typeof(CoyoteInterlocked), call.DeclaringType));
            Assert.All(calls, call => Assert.False(call.IsGenericMethod));
            Assert.Equal(
                new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort) }
                    .SelectMany(type => new[] { type, type }),
                calls.Select(call => call.ReturnType));

            this.Test(InvokeSmallIntegerOverloads);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingExplicitGenericAtomicOverloads()
        {
            IReadOnlyList<MethodInfo> calls = GetAtomicCalls(nameof(InvokeExplicitGenericOverloads));
            Assert.Equal(8, calls.Count);
            Assert.All(calls, call => Assert.Equal(typeof(CoyoteInterlocked), call.DeclaringType));
            Assert.All(calls, call => Assert.True(call.IsGenericMethod));
            Assert.Equal(
                new[] { typeof(string), typeof(int), typeof(ShortEnum), typeof(ULongEnum) }
                    .SelectMany(type => new[] { type, type }),
                calls.Select(call => call.GetGenericArguments().Single()));

            this.Test(InvokeExplicitGenericOverloads);
        }

        private static void InvokeSmallIntegerOverloads()
        {
            byte b = 1;
            Assert.Equal((byte)1, Interlocked.Exchange(ref b, (byte)2));
            Assert.Equal((byte)2, Interlocked.CompareExchange(ref b, (byte)3, (byte)2));
            Assert.Equal((byte)3, b);

            sbyte sb = -1;
            Assert.Equal((sbyte)-1, Interlocked.Exchange(ref sb, (sbyte)-2));
            Assert.Equal((sbyte)-2, Interlocked.CompareExchange(ref sb, (sbyte)-3, (sbyte)-1));
            Assert.Equal((sbyte)-2, sb);

            short s = -1;
            Assert.Equal((short)-1, Interlocked.Exchange(ref s, (short)-2));
            Assert.Equal((short)-2, Interlocked.CompareExchange(ref s, (short)-3, (short)-2));
            Assert.Equal((short)-3, s);

            ushort us = 1;
            Assert.Equal((ushort)1, Interlocked.Exchange(ref us, (ushort)2));
            Assert.Equal((ushort)2, Interlocked.CompareExchange(ref us, (ushort)3, (ushort)1));
            Assert.Equal((ushort)2, us);
        }

        private static void InvokeExplicitGenericOverloads()
        {
            string reference = "old";
            Assert.Equal("old", Interlocked.Exchange<string>(ref reference, "new"));
            Assert.Equal("new", Interlocked.CompareExchange<string>(ref reference, "newer", "new"));
            Assert.Equal("newer", reference);

            int primitive = 1;
            Assert.Equal(1, Interlocked.Exchange<int>(ref primitive, 2));
            Assert.Equal(2, Interlocked.CompareExchange<int>(ref primitive, 3, 1));
            Assert.Equal(2, primitive);

            ShortEnum shortEnum = ShortEnum.First;
            Assert.Equal(ShortEnum.First, Interlocked.Exchange<ShortEnum>(ref shortEnum, ShortEnum.Last));
            Assert.Equal(ShortEnum.Last, Interlocked.CompareExchange<ShortEnum>(
                ref shortEnum, ShortEnum.First, ShortEnum.Last));
            Assert.Equal(ShortEnum.First, shortEnum);

            ULongEnum ulongEnum = ULongEnum.First;
            Assert.Equal(ULongEnum.First, Interlocked.Exchange<ULongEnum>(ref ulongEnum, ULongEnum.Last));
            Assert.Equal(ULongEnum.Last, Interlocked.CompareExchange<ULongEnum>(
                ref ulongEnum, ULongEnum.First, ULongEnum.Last));
            Assert.Equal(ULongEnum.First, ulongEnum);
        }

        private static IReadOnlyList<MethodInfo> GetAtomicCalls(string methodName)
        {
            MethodInfo method = typeof(InterlockedRewritingTests).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Static);
            IReadOnlyList<MethodInfo> calls = method.GetInstructions()
                .Where(instruction => instruction.OpCode == OpCodes.Call && instruction.Operand is MethodInfo)
                .Select(instruction => (MethodInfo)instruction.Operand)
                .Where(call => call.Name is nameof(Interlocked.Exchange) or nameof(Interlocked.CompareExchange))
                .ToArray();
            Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(Interlocked));
            return calls;
        }
    }
}
#endif
