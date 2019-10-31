// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Specifications;
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
            public ActorId Id;

            public Config(ActorId id)
            {
                this.Id = id;
            }
        }

        private class MConfig : Event
        {
            public List<ActorId> Ids;

            public MConfig(List<ActorId> ids)
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

        private class Master : StateMachine
        {
            private List<ActorId> Workers;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Workers = new List<ActorId>();

                for (int idx = 0; idx < 3; idx++)
                {
                    var worker = this.CreateStateMachine(typeof(Worker));
                    this.SendEvent(worker, new Config(this.Id));
                    this.Workers.Add(worker);
                }

                this.Monitor<M>(new MConfig(this.Workers));

                this.RaiseEvent(new Unit());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(FinishedProcessing), nameof(ProcessWorkerIsDone))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                foreach (var worker in this.Workers)
                {
                    this.SendEvent(worker, new DoProcessing());
                }
            }

            private void ProcessWorkerIsDone()
            {
                this.Monitor<M>(new NotifyWorkerIsDone());
            }
        }

        private class Worker : StateMachine
        {
            private ActorId Master;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(Unit), typeof(Processing))]
            private class Init : State
            {
            }

            private void Configure()
            {
                this.Master = (this.ReceivedEvent as Config).Id;
                this.RaiseEvent(new Unit());
            }

            [OnEventGotoState(typeof(DoProcessing), typeof(Done))]
            private class Processing : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            private void DoneOnEntry()
            {
                if (this.Random())
                {
                    this.SendEvent(this.Master, new FinishedProcessing());
                }

                this.RaiseEvent(new Halt());
            }
        }

        private class M : Monitor
        {
            private List<ActorId> Workers;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(MConfig), nameof(Configure))]
            [OnEventGotoState(typeof(Unit), typeof(Done))]
            [OnEventDoAction(typeof(NotifyWorkerIsDone), nameof(ProcessNotification))]
            private class Init : State
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
                    this.RaiseEvent(new Unit());
                }
            }

            private class Done : State
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
                r.CreateStateMachine(typeof(Master));
            },
            configuration: configuration,
            expectedError: "Monitor 'M' detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }
    }
}
