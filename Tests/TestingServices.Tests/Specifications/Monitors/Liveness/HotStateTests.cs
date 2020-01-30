// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Specifications
{
    public class HotStateTests : BaseTest
    {
        public HotStateTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            public ActorId Id;

            public SetupEvent(ActorId id)
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
            [OnEventGotoState(typeof(UnitEvent), typeof(Active))]
            private class Init : State
            {
            }

            private Transition InitOnEntry()
            {
                this.Workers = new List<ActorId>();

                for (int idx = 0; idx < 3; idx++)
                {
                    var worker = this.CreateActor(typeof(Worker));
                    this.SendEvent(worker, new SetupEvent(this.Id));
                    this.Workers.Add(worker);
                }

                this.Monitor<M>(new MConfig(this.Workers));
                return this.RaiseEvent(UnitEvent.Instance);
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
            [OnEventDoAction(typeof(SetupEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Processing))]
            private class Init : State
            {
            }

            private Transition SetupEvent(Event e)
            {
                this.Master = (e as SetupEvent).Id;
                return this.RaiseEvent(UnitEvent.Instance);
            }

            [OnEventGotoState(typeof(DoProcessing), typeof(Done))]
            private class Processing : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            private Transition DoneOnEntry()
            {
                if (this.Random())
                {
                    this.SendEvent(this.Master, new FinishedProcessing());
                }

                return this.Halt();
            }
        }

        private class M : Monitor
        {
            private List<ActorId> Workers;

            [Start]
            [Hot]
            [OnEventDoAction(typeof(MConfig), nameof(SetupEvent))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Done))]
            [OnEventDoAction(typeof(NotifyWorkerIsDone), nameof(ProcessNotification))]
            private class Init : State
            {
            }

            private void SetupEvent(Event e)
            {
                this.Workers = (e as MConfig).Ids;
            }

            private Transition ProcessNotification()
            {
                this.Workers.RemoveAt(0);

                if (this.Workers.Count == 0)
                {
                    return this.RaiseEvent(UnitEvent.Instance);
                }

                return Transition.None;
            }

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestHotStateMonitor()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(M));
                r.CreateActor(typeof(Master));
            },
            configuration: configuration,
            expectedError: "M detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }
    }
}
