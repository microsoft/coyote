// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class CycleDetectionDefaultHandlerTest : BaseTest
    {
        public CycleDetectionDefaultHandlerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Configure : Event
        {
            public bool ApplyFix;

            public Configure(bool applyFix)
            {
                this.ApplyFix = applyFix;
            }
        }

        private class Message : Event
        {
        }

        private class EventHandler : Machine
        {
            private bool ApplyFix;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(Default), nameof(OnDefault))]
            private class Init : MachineState
            {
            }

            private void OnInitEntry()
            {
                this.ApplyFix = (this.ReceivedEvent as Configure).ApplyFix;
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
            private class HotState : MonitorState
            {
            }

            [Cold]
            [OnEventGotoState(typeof(NotifyMessage), typeof(HotState))]
            private class ColdState : MonitorState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestCycleDetectionDefaultHandlerNoBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingIterations = 10;
            configuration.MaxSchedulingSteps = 200;

            this.Test(r =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler), new Configure(true));
            },
            configuration: configuration);
        }

        [Fact(Timeout=5000)]
        public void TestCycleDetectionDefaultHandlerBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.MaxSchedulingSteps = 200;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler), new Configure(false));
            },
            configuration: configuration,
            expectedError: "Monitor 'WatchDog' detected infinite execution that violates a liveness property.",
            replay: true);
        }
    }
}
