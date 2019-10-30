// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
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
            private E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(F), nameof(OnF))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            private void OnF()
            {
                this.e.Tcs.SetResult(true);
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.e.X++;
                return OnExceptionOutcome.HandledException;
            }
        }

        private class M1b : StateMachine
        {
            private E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.e.X++;
                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M2a : StateMachine
        {
            private E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(F), nameof(OnF))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await Task.CompletedTask;
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            private void OnF()
            {
                this.e.Tcs.SetResult(true);
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.e.X++;
                return OnExceptionOutcome.HandledException;
            }
        }

        private class M2b : StateMachine
        {
            private E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await Task.CompletedTask;
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                this.e.X++;
                return OnExceptionOutcome.ThrowException;
            }
        }

        private class M3 : StateMachine
        {
            private E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.e = this.ReceivedEvent as E;
                throw new NotImplementedException();
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                return OnExceptionOutcome.HaltMachine;
            }

            protected override void OnHalt()
            {
                this.e.Tcs.TrySetResult(true);
            }
        }

        private class M4 : StateMachine
        {
            private E e;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.e = this.ReceivedEvent as E;
            }

            protected override OnExceptionOutcome OnException(string methodName, Exception ex)
            {
                if (ex is UnhandledEventException)
                {
                    return OnExceptionOutcome.HaltMachine;
                }

                return OnExceptionOutcome.ThrowException;
            }

            protected override void OnHalt()
            {
                this.e.Tcs.TrySetResult(true);
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
                var m = r.CreateMachine(typeof(M1a), e);
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
                r.CreateMachine(typeof(M1b), e);

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
                var m = r.CreateMachine(typeof(M2a), e);
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
                r.CreateMachine(typeof(M2b), e);

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
                r.CreateMachine(typeof(M3), e);

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
                var m = r.CreateMachine(typeof(M4), e);
                r.SendEvent(m, new F());

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
                Assert.False(failed);
            });
        }
    }
}
