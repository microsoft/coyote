// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    /// <summary>
    /// A single-process implementation of the dining philosophers problem.
    /// </summary>
    public class DiningPhilosophersTest : BaseTest
    {
        public DiningPhilosophersTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Environment : Machine
        {
            private Dictionary<int, MachineId> LockMachines;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.LockMachines = new Dictionary<int, MachineId>();

                int n = 3;
                for (int i = 0; i < n; i++)
                {
                    var lck = this.CreateMachine(typeof(Lock));
                    this.LockMachines.Add(i, lck);
                }

                for (int i = 0; i < n; i++)
                {
                    this.CreateMachine(typeof(Philosopher), new Philosopher.Config(this.LockMachines[i], this.LockMachines[(i + 1) % n]));
                }
            }
        }

        private class Lock : Machine
        {
            public class TryLock : Event
            {
                public MachineId Target;

                public TryLock(MachineId target)
                {
                    this.Target = target;
                }
            }

            public class Release : Event
            {
            }

            public class LockResp : Event
            {
                public bool LockResult;

                public LockResp(bool res)
                {
                    this.LockResult = res;
                }
            }

            private bool LockVar;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            [OnEventDoAction(typeof(TryLock), nameof(OnTryLock))]
            [OnEventDoAction(typeof(Release), nameof(OnRelease))]
            private class Waiting : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.LockVar = false;
                this.Goto<Waiting>();
            }

            private void OnTryLock()
            {
                var target = (this.ReceivedEvent as TryLock).Target;
                if (this.LockVar)
                {
                    this.Send(target, new LockResp(false));
                }
                else
                {
                    this.LockVar = true;
                    this.Send(target, new LockResp(true));
                }
            }

            private void OnRelease()
            {
                this.LockVar = false;
            }
        }

        private class Philosopher : Machine
        {
            public class Config : Event
            {
                public MachineId Left;
                public MachineId Right;

                public Config(MachineId left, MachineId right)
                {
                    this.Left = left;
                    this.Right = right;
                }
            }

            private class TryAgain : Event
            {
            }

            private MachineId left;
            private MachineId right;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(TryAccess))]
            [OnEventDoAction(typeof(TryAgain), nameof(TryAccess))]
            private class Trying : MachineState
            {
            }

            [OnEntry(nameof(OnDone))]
            private class Done : MachineState
            {
            }

            private void InitOnEntry()
            {
                var e = this.ReceivedEvent as Config;
                this.left = e.Left;
                this.right = e.Right;
                this.Goto<Trying>();
            }

            private async Task TryAccess()
            {
                this.Send(this.left, new Lock.TryLock(this.Id));
                var ev = await this.Receive(typeof(Lock.LockResp));
                if ((ev as Lock.LockResp).LockResult)
                {
                    this.Send(this.right, new Lock.TryLock(this.Id));
                    var evr = await this.Receive(typeof(Lock.LockResp));
                    if ((evr as Lock.LockResp).LockResult)
                    {
                        this.Goto<Done>();
                        return;
                    }
                    else
                    {
                        this.Send(this.left, new Lock.Release());
                    }
                }

                this.Send(this.Id, new TryAgain());
            }

            private void OnDone()
            {
                this.Send(this.left, new Lock.Release());
                this.Send(this.right, new Lock.Release());
                this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyDone());
                this.Raise(new Halt());
            }
        }

        private class LivenessMonitor : Monitor
        {
            public class NotifyDone : Event
            {
            }

            [Start]
            [Hot]
            [OnEventGotoState(typeof(NotifyDone), typeof(Done))]
            private class Init : MonitorState
            {
            }

            [Cold]
            [OnEventGotoState(typeof(NotifyDone), typeof(Done))]
            private class Done : MonitorState
            {
            }
        }

        [Theory(Timeout = 10000)]
        [InlineData(469)]
        public void TestDiningPhilosophersLivenessBugWithCycleReplay(int seed)
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.MaxUnfairSchedulingSteps = 100;
            configuration.MaxFairSchedulingSteps = 1000;
            configuration.LivenessTemperatureThreshold = 500;
            configuration.RandomSchedulingSeed = seed;
            configuration.SchedulingIterations = 1;

            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Environment));
            },
            configuration: configuration,
            expectedError: "Monitor 'LivenessMonitor' detected infinite execution that violates a liveness property.",
            replay: true);
        }
    }
}
