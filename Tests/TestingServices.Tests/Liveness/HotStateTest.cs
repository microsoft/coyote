// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Coyote.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class HotStateTest : BaseTest
    {
        public HotStateTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Config : Event
        {
            public MachineId Id;

            public Config(MachineId id)
            {
                this.Id = id;
            }
        }

        private class MConfig : Event
        {
            public List<MachineId> Ids;

            public MConfig(List<MachineId> ids)
            {
                this.Ids = ids;
            }
        }

        private class Unit : Event
        {
        }

        private class DoProcessing : Event
        {
        }

        private class FinishedProcessing : Event
        {
        }

        private class NotifyWorkerIsDone : Event
        {
        }

        private class Master : Machine
        {
            private List<MachineId> Workers;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Workers = new List<MachineId>();

                for (int idx = 0; idx < 3; idx++)
                {
                    var worker = this.CreateMachine(typeof(Worker));
                    this.Send(worker, new Config(this.Id));
                    this.Workers.Add(worker);
                }

                this.Monitor<M>(new MConfig(this.Workers));

                this.Raise(new Unit());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(FinishedProcessing), nameof(ProcessWorkerIsDone))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                foreach (var worker in this.Workers)
                {
                    this.Send(worker, new DoProcessing());
                }
            }

            private void ProcessWorkerIsDone()
            {
                this.Monitor<M>(new NotifyWorkerIsDone());
            }
        }

        private class Worker : Machine
        {
            private MachineId Master;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(Unit), typeof(Processing))]
            private class Init : MachineState
            {
            }

            private void Configure()
            {
                this.Master = (this.ReceivedEvent as Config).Id;
                this.Raise(new Unit());
            }

            [OnEventGotoState(typeof(DoProcessing), typeof(Done))]
            private class Processing : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            private void DoneOnEntry()
            {
                if (this.Random())
                {
                    this.Send(this.Master, new FinishedProcessing());
                }

                this.Raise(new Halt());
            }
        }

        private class M : Monitor
        {
            private List<MachineId> Workers;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(MConfig), nameof(Configure))]
            [OnEventGotoState(typeof(Unit), typeof(Done))]
            [OnEventDoAction(typeof(NotifyWorkerIsDone), nameof(ProcessNotification))]
            private class Init : MonitorState
            {
            }

            private void Configure()
            {
                this.Workers = (this.ReceivedEvent as MConfig).Ids;
            }

            private void ProcessNotification()
            {
                this.Workers.RemoveAt(0);

                if (this.Workers.Count == 0)
                {
                    this.Raise(new Unit());
                }
            }

            private class Done : MonitorState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestHotStateMonitor()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(M));
                r.CreateMachine(typeof(Master));
            },
            configuration: configuration,
            expectedError: "Monitor 'M' detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }
    }
}
