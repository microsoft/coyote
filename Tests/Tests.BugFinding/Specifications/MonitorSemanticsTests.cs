// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.Specifications
{
    public class MonitorSemanticsTests : BaseBugFindingTest
    {
        public MonitorSemanticsTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Notify : Event
        {
        }

        private class M1 : Monitor
        {
            private int Value = 0;
            private object SyncObject = new object();

            [Start]
            [OnEventDoAction(typeof(Notify), nameof(HandleNotify))]
            private class Init : State
            {
            }

            private void HandleNotify()
            {
                int value = ++this.Value;
                lock (this.SyncObject)
                {
                    this.Assert(value == this.Value, "Found unexpected value.");
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSchedulingPointsDuringMonitor()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<M1>();

                Task t1 = Task.Run(() =>
                {
                    Specification.Monitor<M1>(new Notify());
                });

                Task t2 = Task.Run(() =>
                {
                    Specification.Monitor<M1>(new Notify());
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(50));
        }

        private class M2 : Monitor
        {
            private int Value = 0;
            private object SyncObject = new object();

            [Start]
            [OnEventDoAction(typeof(Notify), nameof(HandleNotify))]
            private class Init : State
            {
            }

            private void HandleNotify()
            {
                int value = ++this.Value;
                lock (this.SyncObject)
                {
                    this.Assert(value == this.Value, "Found unexpected value.");
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSuppressedSchedulingPointsDuringMonitor()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<M2>();

                Task t1 = Task.Run(() =>
                {
                    Specification.Monitor<M2>(new Notify());
                });

                Task t2 = Task.Run(() =>
                {
                    Specification.Monitor<M2>(new Notify());
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(50));
        }
    }
}
