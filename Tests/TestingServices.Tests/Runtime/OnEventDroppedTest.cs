// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class OnEventDroppedTest : BaseTest
    {
        public OnEventDroppedTest(ITestOutputHelper output)
            : base(output)
        {
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
            private class Init : MachineState
            {
            }

            protected override void OnHalt()
            {
                this.Send(this.Id, new E());
            }
        }

        [Fact(Timeout=5000)]
        public void TestOnDroppedCalled1()
        {
            this.TestWithError(r =>
            {
                r.OnEventDropped += (e, target) =>
                {
                    r.Assert(false);
                };

                var m = r.CreateMachine(typeof(M1));
                r.SendEvent(m, new Halt());
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new Halt());
                this.Send(this.Id, new E());
            }
        }

        [Fact(Timeout=5000)]
        public void TestOnDroppedCalled2()
        {
            this.TestWithError(r =>
            {
                r.OnEventDropped += (e, target) =>
                {
                    r.Assert(false);
                };

                var m = r.CreateMachine(typeof(M2));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOnDroppedParams()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M1));

                r.OnEventDropped += (e, target) =>
                {
                    r.Assert(e is E);
                    r.Assert(target == m);
                };

                r.SendEvent(m, new Halt());
            });
        }

        private class EventProcessed : Event
        {
        }

        private class EventDropped : Event
        {
        }

        private class Monitor3 : Monitor
        {
            [Hot]
            [Start]
            [OnEventGotoState(typeof(EventProcessed), typeof(S2))]
            [OnEventGotoState(typeof(EventDropped), typeof(S2))]
            private class S1 : MonitorState
            {
            }

            [Cold]
            private class S2 : MonitorState
            {
            }
        }

        private class M3a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send((this.ReceivedEvent as E).Id, new Halt());
            }
        }

        private class M3b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send((this.ReceivedEvent as E).Id, new E());
            }
        }

        private class M3c : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Processed))]
            private class Init : MachineState
            {
            }

            private void Processed()
            {
                this.Monitor<Monitor3>(new EventProcessed());
            }
        }

        [Fact(Timeout=5000)]
        public void TestProcessedOrDropped()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Monitor3));
                r.OnEventDropped += (e, target) =>
                {
                    r.InvokeMonitor(typeof(Monitor3), new EventDropped());
                };

                var m = r.CreateMachine(typeof(M3c));
                r.CreateMachine(typeof(M3a), new E(m));
                r.CreateMachine(typeof(M3b), new E(m));
            },
            configuration: Configuration.Create().WithNumberOfIterations(1000));
        }
    }
}
