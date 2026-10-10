// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Mono.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    /// <summary>
    /// Tests the overloads that the runtime API gate discovered without a controlled replacement.
    /// </summary>
    public class TimeoutOverloadRewritingTests : BaseRewritingTest
    {
        public TimeoutOverloadRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingMonitorTryEnterWithMillisecondsTimeout()
        {
            Assert.Equal(typeof(Microsoft.Coyote.Rewriting.Types.Threading.Monitor),
                GetCall(nameof(TryEnterWithMillisecondsTimeout), nameof(Monitor.TryEnter)).DeclaringType);

            this.Test(async () =>
            {
                var sync = new object();
                int value = 0;
                Task[] tasks = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
                {
                    Assert.True(TryEnterWithMillisecondsTimeout(sync));
                    try
                    {
                        int old = value;
                        Thread.Yield();
                        value = old + 1;
                    }
                    finally
                    {
                        Monitor.Exit(sync);
                    }
                })).ToArray();
                await Task.WhenAll(tasks);
                Assert.Equal(2, value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(50));
        }

#if NET7_0_OR_GREATER
        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWaitWithTimeSpanAndCancellationToken()
        {
            Assert.Equal(typeof(Microsoft.Coyote.Rewriting.Types.Threading.Tasks.Task),
                GetCall(nameof(WaitWithTimeSpanAndCancellationToken), nameof(Task.Wait)).DeclaringType);

            this.Test(() =>
            {
                var tcs = new TaskCompletionSource<bool>();
                Task producer = Task.Run(() => tcs.SetResult(true));
                Assert.True(WaitWithTimeSpanAndCancellationToken(tcs.Task));
                producer.Wait();
            },
            configuration: this.GetConfiguration().WithTestingIterations(50));
        }

        private static bool WaitWithTimeSpanAndCancellationToken(Task task)
        {
            using var cts = new CancellationTokenSource();
            return task.Wait(TimeSpan.FromMinutes(10), cts.Token);
        }
#endif

        private static bool TryEnterWithMillisecondsTimeout(object sync) => Monitor.TryEnter(sync, 100);

        private static MethodInfo GetCall(string methodName, string calleeName) =>
            typeof(TimeoutOverloadRewritingTests).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                .GetInstructions()
                .Where(instruction => (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                    instruction.Operand is MethodInfo)
                .Select(instruction => (MethodInfo)instruction.Operand)
                .Single(call => call.Name == calleeName);
    }
}
