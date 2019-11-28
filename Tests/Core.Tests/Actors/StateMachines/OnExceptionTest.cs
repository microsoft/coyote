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
    public class OnExceptionTest : BaseTest
    {
        public OnExceptionTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public int X;
            public TaskCompletionSource<bool> Tcs;

            public E(TaskCompletionSource<bool> tcs)
            {
                this.X = 0;
                this.Tcs = tcs;
            }
        }

        private class F : Event
        {
        }

        private class M1a : StateMachine
        {
            private E Event;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(F), nameof(OnF))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Event = e as E;
                throw new NotImplementedException();
            }

            private void OnF()
            {
                this.Event.Tcs.SetResult(true);
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                this.Event.X++;
                return OnExceptionOutcome.HandledException;
            }
        }

        private class M1b : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                (e as E).X++;
                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M2a : StateMachine
        {
            private E Event;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(F), nameof(OnF))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                await Task.CompletedTask;
                this.Event = e as E;
                throw new NotImplementedException();
            }

            private void OnF()
            {
                this.Event.Tcs.SetResult(true);
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                (e as E).X++;
                return OnExceptionOutcome.HandledException;
            }
        }

        private class M2b : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                await Task.CompletedTask;
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                (e as E).X++;
                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                return OnExceptionOutcome.Halt;
            }

            protected override Task OnHaltAsync(Event e)
            {
                (e as E).Tcs.TrySetResult(true);
                return Task.CompletedTask;
            }
        }

        private class M4 : StateMachine
        {
            private E Event;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Event = e as E;
            }

            protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
            {
                if (ex is UnhandledEventException)
                {
                    return OnExceptionOutcome.Halt;
                }

                return OnExceptionOutcome.ThrowException;
            }

            protected override Task OnHaltAsync(Event e)
            {
                this.Assert(e is F);
                this.Event.Tcs.TrySetResult(true);
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout=5000)]
        public async Task TestOnExceptionCalledOnce1()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    Assert.True(false);
                    failed = true;
                    tcs.SetResult(true);
                };

                var e = new E(tcs);
                var m = r.CreateActor(typeof(M1a), e);
                r.SendEvent(m, new F());

                await WaitAsync(tcs.Task);
                Assert.False(failed);
                Assert.True(e.X == 1);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestOnExceptionCalledOnce2()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(true);
                };

                var e = new E(tcs);
                r.CreateActor(typeof(M1b), e);

                await WaitAsync(tcs.Task);
                Assert.True(failed);
                Assert.True(e.X == 1);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestOnExceptionCalledOnceAsync1()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    Assert.True(false);
                    failed = true;
                    tcs.SetResult(true);
                };

                var e = new E(tcs);
                var m = r.CreateActor(typeof(M2a), e);
                r.SendEvent(m, new F());

                await WaitAsync(tcs.Task);
                Assert.False(failed);
                Assert.True(e.X == 1);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestOnExceptionCalledOnceAsync2()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(true);
                };

                var e = new E(tcs);
                r.CreateActor(typeof(M2b), e);

                await WaitAsync(tcs.Task);
                Assert.True(failed);
                Assert.True(e.X == 1);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestOnExceptionCanHalt()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.TrySetResult(false);
                };

                var e = new E(tcs);
                r.CreateActor(typeof(M3), e);

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
                Assert.False(failed);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestUnHandledEventCanHalt()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.TrySetResult(false);
                };

                var e = new E(tcs);
                var m = r.CreateActor(typeof(M4), e);
                r.SendEvent(m, new F());

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
                Assert.False(failed);
            });
        }
    }
}
