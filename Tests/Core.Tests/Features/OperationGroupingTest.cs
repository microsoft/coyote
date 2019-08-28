// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class OperationGroupingTest : BaseTest
    {
        public OperationGroupingTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private static Guid OperationGroup1 = Guid.NewGuid();
        private static Guid OperationGroup2 = Guid.NewGuid();

        private class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class SetupMultipleEvent : Event
        {
            public TaskCompletionSource<bool>[] Tcss;

            public SetupMultipleEvent(params TaskCompletionSource<bool>[] tcss)
            {
                this.Tcss = tcss;
            }
        }

        private class E : Event
        {
            public MachineId Id;

            public E()
            {
            }

            public E(MachineId id)
            {
                this.Id = id;
            }
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                tcs.SetResult(this.OperationGroupId == Guid.Empty);
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestOperationGroupingSingleMachineNoSend()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M1), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        private class M2 : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Send(this.Id, new E());
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == Guid.Empty);
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestOperationGroupingSingleMachineSend()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M2), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        private class M3 : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Runtime.SendEvent(this.Id, new E(), OperationGroup1);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup1);
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestOperationGroupingSingleMachineSendStarter()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M3), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        private class M4A : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.CreateMachine(typeof(M4B), new SetupEvent(tcs));
            }
        }

        private class M4B : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                tcs.SetResult(this.OperationGroupId == Guid.Empty);
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestOperationGroupingTwoMachinesCreate()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M4A), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        private class M5A : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                var target = this.CreateMachine(typeof(M5B), new SetupEvent(tcs));
                this.Send(target, new E());
            }
        }

        private class M5B : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == Guid.Empty);
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestOperationGroupingTwoMachinesSend()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M5A), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        private class M6A : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                var target = this.CreateMachine(typeof(M6B), new SetupEvent(tcs));
                this.Runtime.SendEvent(target, new E(), OperationGroup1);
            }
        }

        private class M6B : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup1);
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestOperationGroupingTwoMachinesSendStarter()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M6A), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        private class M7A : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcss = (this.ReceivedEvent as SetupMultipleEvent).Tcss;
                this.Tcs = tcss[0];
                var target = this.CreateMachine(typeof(M7B), new SetupEvent(tcss[1]));
                this.Runtime.SendEvent(target, new E(this.Id), OperationGroup1);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup1);
            }
        }

        private class M7B : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup1);
                this.Send((this.ReceivedEvent as E).Id, new E());
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestOperationGroupingTwoMachinesSendBack()
        {
            await this.RunAsync(async r =>
            {
                var tcs1 = new TaskCompletionSource<bool>();
                var tcs2 = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M7A), new SetupMultipleEvent(tcs1, tcs2));

                var result = await GetResultAsync(tcs1.Task);
                Assert.True(result);

                result = await GetResultAsync(tcs2.Task);
                Assert.True(result);
            });
        }

        private class M8A : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcss = (this.ReceivedEvent as SetupMultipleEvent).Tcss;
                this.Tcs = tcss[0];
                var target = this.CreateMachine(typeof(M8B), new SetupEvent(tcss[1]));
                this.Runtime.SendEvent(target, new E(this.Id), OperationGroup1);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup2);
            }
        }

        private class M8B : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup1);
                this.Runtime.SendEvent((this.ReceivedEvent as E).Id, new E(), OperationGroup2);
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestOperationGroupingTwoMachinesSendBackStarter()
        {
            await this.RunAsync(async r =>
            {
                var tcs1 = new TaskCompletionSource<bool>();
                var tcs2 = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M8A), new SetupMultipleEvent(tcs1, tcs2));

                var result = await GetResultAsync(tcs1.Task);
                Assert.True(result);

                result = await GetResultAsync(tcs2.Task);
                Assert.True(result);
            });
        }

        private class M9A : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcss = (this.ReceivedEvent as SetupMultipleEvent).Tcss;
                this.Tcs = tcss[0];
                var target = this.CreateMachine(typeof(M9B), new SetupMultipleEvent(tcss[1], tcss[2]));
                this.Runtime.SendEvent(target, new E(this.Id), OperationGroup1);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup2);
            }
        }

        private class M9B : Machine
        {
            private TaskCompletionSource<bool> Tcs;
            private TaskCompletionSource<bool> TargetTcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcss = (this.ReceivedEvent as SetupMultipleEvent).Tcss;
                this.Tcs = tcss[0];
                this.TargetTcs = tcss[1];
            }

            private void CheckEvent()
            {
                this.CreateMachine(typeof(M9C), new SetupEvent(this.TargetTcs));
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup1);
                this.Runtime.SendEvent((this.ReceivedEvent as E).Id, new E(), OperationGroup2);
            }
        }

        private class M9C : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                tcs.SetResult(this.OperationGroupId == OperationGroup1);
            }
        }

        [Fact(Timeout=5000)]
        public async Task TestOperationGroupingThreeMachinesSendStarter()
        {
            await this.RunAsync(async r =>
            {
                var tcs1 = new TaskCompletionSource<bool>();
                var tcs2 = new TaskCompletionSource<bool>();
                var tcs3 = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M9A), new SetupMultipleEvent(tcs1, tcs2, tcs3));

                var result = await GetResultAsync(tcs1.Task);
                Assert.True(result);

                result = await GetResultAsync(tcs2.Task);
                Assert.True(result);

                result = await GetResultAsync(tcs3.Task);
                Assert.True(result);
            });
        }
    }
}
