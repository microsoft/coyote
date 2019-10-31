// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class ExceptionPropagationTest : BaseTest
    {
        public ExceptionPropagationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Configure : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public Configure(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Configure).Tcs;
                try
                {
                    this.Assert(false);
                }
                finally
                {
                    tcs.SetResult(true);
                }
            }
        }

        private class N : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as Configure).Tcs;
                try
                {
                    throw new InvalidOperationException();
                }
                finally
                {
                    tcs.SetResult(true);
                }
            }
        }

        [Fact(Timeout=5000)]
        public async Task TestAssertFailureNoEventHandler()
        {
            var runtime = ActorRuntimeFactory.Create();
            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(M), new Configure(tcs));
            await tcs.Task;
        }

        [Fact(Timeout=5000)]
        public async Task TestAssertFailureEventHandler()
        {
            await this.RunAsync(async r =>
            {
                var tcsFail = new TaskCompletionSource<bool>();
                int count = 0;

                r.OnFailure += (exception) =>
                {
                    if (!(exception is MachineActionExceptionFilterException))
                    {
                        count++;
                        tcsFail.SetException(exception);
                    }
                };

                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M), new Configure(tcs));

                await WaitAsync(tcs.Task);

                AssertionFailureException ex = await Assert.ThrowsAsync<AssertionFailureException>(async () => await tcsFail.Task);
                Assert.Equal(1, count);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestUnhandledExceptionEventHandler()
        {
            await this.RunAsync(async r =>
            {
                var tcsFail = new TaskCompletionSource<bool>();
                int count = 0;
                bool sawFilterException = false;

                r.OnFailure += (exception) =>
                {
                    // This test throws an exception that we should receive a filter call for
                    if (exception is MachineActionExceptionFilterException)
                    {
                        sawFilterException = true;
                        return;
                    }

                    count++;
                    tcsFail.SetException(exception);
                };

                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(N), new Configure(tcs));

                await WaitAsync(tcs.Task);

                AssertionFailureException ex = await Assert.ThrowsAsync<AssertionFailureException>(async () => await tcsFail.Task);
                Assert.IsType<InvalidOperationException>(ex.InnerException);
                Assert.Equal(1, count);
                Assert.True(sawFilterException);
            });
        }
    }
}
