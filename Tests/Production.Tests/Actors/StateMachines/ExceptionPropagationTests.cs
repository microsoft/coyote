// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests
{
    public class ExceptionPropagationTests : BaseProductionTest
    {
        public ExceptionPropagationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEvent(TaskCompletionSource<bool> tcs)
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

            private void InitOnEntry(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
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

            private void InitOnEntry(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
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

        [Fact(Timeout = 5000)]
        public async Task TestAssertFailureNoEventHandler()
        {
            var runtime = ActorRuntimeFactory.Create();
            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateActor(typeof(M), new SetupEvent(tcs));
            await tcs.Task;
        }

        [Fact(Timeout = 5000)]
        public async Task TestAssertFailureEventHandler()
        {
            await this.RunAsync(async r =>
            {
                var tcsFail = new TaskCompletionSource<bool>();
                int count = 0;

                r.OnFailure += (exception) =>
                {
                    if (!(exception is ActionExceptionFilterException))
                    {
                        count++;
                        tcsFail.SetException(exception);
                    }
                };

                var tcs = new TaskCompletionSource<bool>();
                r.CreateActor(typeof(M), new SetupEvent(tcs));

                await WaitAsync(tcs.Task);

                AssertionFailureException ex = await Assert.ThrowsAsync<AssertionFailureException>(async () => await tcsFail.Task);
                Assert.Equal(1, count);
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUnhandledExceptionEventHandler()
        {
            await this.RunAsync(async r =>
            {
                var tcsFail = new TaskCompletionSource<bool>();
                int count = 0;
                bool sawFilterException = false;

                r.OnFailure += (exception) =>
                {
                    // The "N" machine throws a InvalidOperationException which we should receive
                    // here wrapped in ActionExceptionFilterException for the OnFailure callback.

                    if (exception is ActionExceptionFilterException)
                    {
                        sawFilterException = true;
                        return;
                    }

                    count++;
                    tcsFail.SetException(exception);
                };

                var tcs = new TaskCompletionSource<bool>();
                r.CreateActor(typeof(N), new SetupEvent(tcs));

                await WaitAsync(tcs.Task);

                AssertionFailureException ex = await Assert.ThrowsAsync<AssertionFailureException>(async () => await tcsFail.Task);
                Assert.IsType<InvalidOperationException>(ex.InnerException);
                Assert.Equal(1, count);
                Assert.True(sawFilterException);
            });
        }
    }
}
