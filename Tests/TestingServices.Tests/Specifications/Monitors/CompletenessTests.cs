// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Specifications
{
    public class CompletenessTests : BaseTest
    {
        public CompletenessTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class P : Monitor
        {
            [Cold]
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Fail))]
            [OnEventGotoState(typeof(E2), typeof(S2))]
            private class S1 : State
            {
            }

            [Cold]
            [IgnoreEvents(typeof(E1), typeof(E2))]
            private class S2 : State
            {
            }

            private void Fail()
            {
                this.Assert(false);
            }
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class S : State
            {
            }

            private void InitOnEntry()
            {
                this.Monitor<P>(new E1());
            }
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class S : State
            {
            }

            private void InitOnEntry()
            {
                this.Monitor<P>(new E2());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCompleteness1()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(P));
                r.CreateActor(typeof(M2));
                r.CreateActor(typeof(M1));
            },
            configuration: Configuration.Create().WithNumberOfIterations(100),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCompleteness2()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(P));
                r.CreateActor(typeof(M1));
                r.CreateActor(typeof(M2));
            },
            configuration: Configuration.Create().WithNumberOfIterations(100),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }
    }
}
