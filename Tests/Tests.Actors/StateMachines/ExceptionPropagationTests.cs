// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Actors.Tests
{
    public class ExceptionPropagationTests : BaseActorTest
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

#pragma warning disable CA1822 // Mark members as static
            private void InitOnEntry(Event e)
#pragma warning restore CA1822 // Mark members as static
            {
                var tcs = (e as SetupEvent).Tcs;
                try
                {
                    ThrowException<InvalidOperationException>();
                }
                finally
                {
                    tcs.SetResult(true);
                }
            }
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAssertFailureNoEventHandler()
        {
            var runtime = RuntimeFactory.Create();
            var tcs = TaskCompletionSource.Create<bool>();
            runtime.CreateActor(typeof(M), new SetupEvent(tcs));
            await tcs.Task;
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAssertFailureEventHandler()
        {
            await this.RunAsync(async r =>
            {
                var tcsFail = TaskCompletionSource.Create<bool>();
                int count = 0;

                r.OnFailure += (exception) =>
                {
                    if (!(exception is ActionExceptionFilterException))
                    {
                        count++;
                        tcsFail.SetException(exception);
                    }
                };

                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M), new SetupEvent(tcs));

                AssertionFailureException ex = await Assert.ThrowsAsync<AssertionFailureException>(
                    async () => await this.WaitAsync(tcsFail.Task));
                Assert.Equal(1, count);
            },
            handleFailures: false);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestUnhandledExceptionEventHandler()
        {
            await this.RunAsync(async r =>
            {
                var tcsFail = TaskCompletionSource.Create<bool>();
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

                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(N), new SetupEvent(tcs));

                await this.WaitAsync(tcs.Task);

                AssertionFailureException ex = await Assert.ThrowsAsync<AssertionFailureException>(async () => await tcsFail.Task);
                Assert.IsType<InvalidOperationException>(ex.InnerException);
                Assert.Equal(1, count);
                Assert.True(sawFilterException);
            },
            handleFailures: false);
        }
    }
}
