// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Mono.Reflection;
using Xunit;
using Xunit.Abstractions;
using CoyoteVolatile = Microsoft.Coyote.Rewriting.Types.Threading.Volatile;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class VolatileRewritingTests : BaseRewritingTest
    {
        public VolatileRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingLeavesMemoryBarriersAsRuntimeCalls()
        {
            MethodInfo[] calls = typeof(VolatileRewritingTests)
                .GetMethod(nameof(InvokeBarriersAndVolatileAccesses), BindingFlags.NonPublic | BindingFlags.Static)
                .GetInstructions()
                .Where(instruction => instruction.OpCode == OpCodes.Call && instruction.Operand is MethodInfo)
                .Select(instruction => (MethodInfo)instruction.Operand)
                .Where(call => call.DeclaringType == typeof(Volatile) || call.DeclaringType == typeof(CoyoteVolatile))
                .ToArray();

            // The barriers are deliberately invoked natively, whereas the volatile accesses are controlled.
            Assert.Equal(
                new[]
                {
                    (typeof(Volatile), nameof(Volatile.ReadBarrier)),
                    (typeof(CoyoteVolatile), nameof(Volatile.Write)),
                    (typeof(Volatile), nameof(Volatile.WriteBarrier)),
                    (typeof(CoyoteVolatile), nameof(Volatile.Read))
                },
                calls.Select(call => (call.DeclaringType, call.Name)));

            this.Test(() => Assert.Equal(42, InvokeBarriersAndVolatileAccesses()));
        }

        private static int InvokeBarriersAndVolatileAccesses()
        {
            int value = 0;
            Volatile.ReadBarrier();
            Volatile.Write(ref value, 42);
            Volatile.WriteBarrier();
            return Volatile.Read(ref value);
        }
    }
}
#endif
