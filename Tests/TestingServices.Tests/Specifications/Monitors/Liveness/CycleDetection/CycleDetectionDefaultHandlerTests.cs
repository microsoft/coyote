// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Specifications
{
    public class CycleDetectionDefaultHandlerTests : BaseTest
    {
        public CycleDetectionDefaultHandlerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            public bool ApplyFix;

            public SetupEvent(bool applyFix)
            {
                this.ApplyFix = applyFix;
            }
        }

        private class Message : Event
        {
        }

        private class EventHandler : StateMachine
        {
            private bool ApplyFix;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(DefaultEvent), nameof(OnDefault))]
            private class Init : State
            {
            }

            private void OnInitEntry(Event e)
            {
                this.ApplyFix = (e as SetupEvent).ApplyFix;
            }

            private void OnDefault()
            {
                if (this.ApplyFix)
                {
                    this.Monitor<WatchDog>(new WatchDog.NotifyMessage());
                }
            }
        }

        private class WatchDog : Monitor
        {
            public class NotifyMessage : Event
            {
            }

            [Start]
            [Hot]
            [OnEventGotoState(typeof(NotifyMessage), typeof(ColdState))]
            private class HotState : State
            {
            }

            [Cold]
            [OnEventGotoState(typeof(NotifyMessage), typeof(HotState))]
            private class ColdState : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCycleDetectionDefaultHandlerNoBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingIterations = 10;
            configuration.MaxSchedulingSteps = 200;

            this.Test(r =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateActor(typeof(EventHandler), new SetupEvent(true));
            },
            configuration: configuration);
        }

        [Fact(Timeout = 5000)]
        public void TestCycleDetectionDefaultHandlerBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.MaxSchedulingSteps = 200;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateActor(typeof(EventHandler), new SetupEvent(false));
            },
            configuration: configuration,
            expectedError: "Monitor 'WatchDog' detected infinite execution that violates a liveness property.",
            replay: true);
        }
    }
}
