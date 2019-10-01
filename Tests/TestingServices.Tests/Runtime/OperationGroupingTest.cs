// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class OperationGroupingTest : BaseTest
    {
        public OperationGroupingTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private static Guid OperationGroup1 = Guid.NewGuid();
        private static Guid OperationGroup2 = Guid.NewGuid();

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
                var id = this.OperationGroupId;
                this.Assert(id == Guid.Empty, $"Operation group id is not '{Guid.Empty}', but {id}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingSingleMachineNoSend()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M1));
            });
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == Guid.Empty, $"Operation group id is not '{Guid.Empty}', but {id}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingSingleMachineSend()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M2));
            });
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Runtime.SendEvent(this.Id, new E(), OperationGroup1);
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingSingleMachineSendStarter()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M3));
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
                this.CreateMachine(typeof(M4B));
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
                var id = this.OperationGroupId;
                this.Assert(id == Guid.Empty, $"Operation group id is not '{Guid.Empty}', but {id}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingTwoMachinesCreate()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M4A));
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
                var target = this.CreateMachine(typeof(M5B));
                this.Send(target, new E());
            }
        }

        private class M5B : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == Guid.Empty, $"Operation group id is not '{Guid.Empty}', but {id}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingTwoMachinesSend()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M5A));
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
                var target = this.CreateMachine(typeof(M6B));
                this.Runtime.SendEvent(target, new E(), OperationGroup1);
            }
        }

        private class M6B : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingTwoMachinesSendStarter()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M6A));
            });
        }

        private class M7A : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var target = this.CreateMachine(typeof(M7B));
                this.Runtime.SendEvent(target, new E(this.Id), OperationGroup1);
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
            }
        }

        private class M7B : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
                this.Send((this.ReceivedEvent as E).Id, new E());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingTwoMachinesSendBack()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M7A));
            });
        }

        private class M8A : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var target = this.CreateMachine(typeof(M8B));
                this.Runtime.SendEvent(target, new E(this.Id), OperationGroup1);
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup2, $"Operation group id is not '{OperationGroup2}', but {id}.");
            }
        }

        private class M8B : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
                this.Runtime.SendEvent((this.ReceivedEvent as E).Id, new E(), OperationGroup2);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingTwoMachinesSendBackStarter()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M8A));
            });
        }

        private class M9A : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var target = this.CreateMachine(typeof(M9B));
                this.Runtime.SendEvent(target, new E(this.Id), OperationGroup1);
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup2, $"Operation group id is not '{OperationGroup2}', but {id}.");
            }
        }

        private class M9B : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void CheckEvent()
            {
                this.CreateMachine(typeof(M9C));
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
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
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
            }
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingThreeMachinesSendStarter()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M9A));
            });
        }

        private class M10 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E), typeof(Final))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(FinalOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Check))]
            private class Final : MachineState
            {
            }

            private void InitOnEntry()
            {
                var e = new E(this.Id);
                this.Send(this.Id, e, OperationGroup1);
                this.Runtime.SendEvent(this.Id, e, OperationGroup2);
            }

            private void FinalOnEntry()
            {
                this.Assert(this.OperationGroupId == OperationGroup1,
                    $"[1] Operation group id is not '{OperationGroup1}', but {this.OperationGroupId}.");
            }

            private void Check()
            {
                this.Assert(this.OperationGroupId == OperationGroup2,
                    $"[2] Operation group id is not '{OperationGroup2}', but {this.OperationGroupId}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingSendSameEventWithOtherOpId()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M10));
            });
        }

        private class M11 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E), typeof(Final))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(FinalOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Check))]
            private class Final : MachineState
            {
            }

            private void InitOnEntry()
            {
                var e = new E(this.Id);
                this.Send(this.Id, e, OperationGroup1);
                this.OperationGroupId = OperationGroup2;
                this.Runtime.SendEvent(this.Id, e);
            }

            private void FinalOnEntry()
            {
                this.Assert(this.OperationGroupId == OperationGroup1,
                    $"[1] Operation group id is not '{OperationGroup1}', but {this.OperationGroupId}.");
            }

            private void Check()
            {
                this.Assert(this.OperationGroupId == OperationGroup2,
                    $"[2] Operation group id is not '{OperationGroup2}', but {this.OperationGroupId}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingSendSameEventWithOtherMachineOpId()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M11));
            });
        }

        private class M12 : Machine
        {
            private E Event;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E), typeof(Intermediate))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(IntermediateOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Intermediate : MachineState
            {
            }

            [OnEntry(nameof(FinalOnEntry))]
            private class Final : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Event = new E(this.Id);
                this.Raise(this.Event, OperationGroup1);
            }

            private void IntermediateOnEntry()
            {
                this.Assert(this.OperationGroupId == OperationGroup1,
                    $"[1] Operation group id is not '{OperationGroup1}', but {this.OperationGroupId}.");
                this.Raise(this.Event, OperationGroup2);
            }

            private void FinalOnEntry()
            {
                this.Assert(this.OperationGroupId == OperationGroup2,
                    $"[2] Operation group id is not '{OperationGroup2}', but {this.OperationGroupId}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingRaiseSameEventWithOtherOpId()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M12));
            });
        }

        private class M13 : Machine
        {
            private E Event;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E), typeof(Intermediate))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(IntermediateOnEntry))]
            [IgnoreEvents(typeof(E))]
            private class Intermediate : MachineState
            {
            }

            [OnEntry(nameof(FinalOnEntry))]
            private class Final : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Event = new E(this.Id);
                this.Raise(this.Event, OperationGroup1);
            }

            private void IntermediateOnEntry()
            {
                this.Assert(this.OperationGroupId == OperationGroup1,
                    $"[1] Operation group id is not '{OperationGroup1}', but {this.OperationGroupId}.");
                this.OperationGroupId = OperationGroup2;
                this.Assert(this.OperationGroupId == OperationGroup2,
                    $"[2] Operation group id is not '{OperationGroup2}', but {this.OperationGroupId}.");
                this.Raise(this.Event);
            }

            private void FinalOnEntry()
            {
                this.Assert(this.OperationGroupId == OperationGroup2,
                    $"[3] Operation group id is not '{OperationGroup2}', but {this.OperationGroupId}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingRaiseSameEventWithOtherMachineOpId()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M13));
            });
        }

        private class M14 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                this.Send(this.Id, new E(this.Id), OperationGroup1);
                this.Assert(this.OperationGroupId == Guid.Empty,
                    $"[1] Operation group id is not '{Guid.Empty}', but {this.OperationGroupId}.");
                await this.Receive(typeof(E));
                this.Assert(this.OperationGroupId == OperationGroup1,
                    $"[2] Operation group id is not '{OperationGroup1}', but {this.OperationGroupId}.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingReceivedEvent()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M14));
            });
        }
    }
}
