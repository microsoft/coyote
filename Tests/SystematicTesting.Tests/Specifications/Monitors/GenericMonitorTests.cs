// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Specifications
{
    public class GenericMonitorTests : BaseSystematicTest
    {
        public GenericMonitorTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Program<T> : StateMachine
        {
            private T Item;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Item = default;
                this.RaiseGotoStateEvent<Active>();
            }

            [OnEntry(nameof(ActiveInit))]
            private class Active : State
            {
            }

            private void ActiveInit()
            {
                this.Assert(this.Item is int);
            }
        }

        private class M<T> : Monitor
        {
            [Start]
            [OnEntry(nameof(Init))]
            private class S1 : State
            {
            }

            private class S2 : State
            {
            }

            private void Init() => this.RaiseGotoStateEvent<S2>();
        }

        [Fact(Timeout = 5000)]
        public void TestGenericMonitor()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(M<int>));
                r.CreateActor(typeof(Program<int>));
            });
        }
    }
}
