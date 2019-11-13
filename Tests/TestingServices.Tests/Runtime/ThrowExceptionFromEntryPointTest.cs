// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Runtime
{
    public class ThrowExceptionFromEntryPointTest : BaseTest
    {
        public ThrowExceptionFromEntryPointTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : StateMachine
        {
            [Start]
            private class Init : State
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestThrowExceptionTestFromEntryPoint()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                ActorId m = r.CreateActor(typeof(M));
                throw new InvalidOperationException();
            },
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestThrowExceptionTestFromEntryPointNoMachines()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                throw new InvalidOperationException();
            },
            replay: true);
        }
    }
}
