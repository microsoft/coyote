// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class IgnoreRaisedTest : BaseTest
    {
        public IgnoreRaisedTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
            public MachineId Mid;

            public E2(MachineId mid)
            {
                this.Mid = mid;
            }
        }

        private class Unit : Event
        {
        }

        private class A : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [IgnoreEvents(typeof(Unit))]
            [OnEventDoAction(typeof(E2), nameof(Bar))]
            private class Init : MachineState
            {
            }

            private void Foo()
            {
                this.Raise(new Unit());
            }

            private void Bar()
            {
                var e = this.ReceivedEvent as E2;
                this.Send(e.Mid, new E2(this.Id));
            }
        }

        private class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var m = this.CreateMachine(typeof(A));
                this.Send(m, new E1());
                this.Send(m, new E2(this.Id));
                var e = await this.Receive(typeof(E2)) as E2;

                // Console.WriteLine("Got Response from {0}", e.mid);
            }
        }

        /// <summary>
        /// Coyote semantics test: testing for ignore of a raised event.
        /// </summary>
        [Fact(Timeout=5000)]
        public void TestIgnoreRaisedEventHandled()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(Harness));
            },
            configuration: GetConfiguration().WithNumberOfIterations(5));
        }
    }
}
