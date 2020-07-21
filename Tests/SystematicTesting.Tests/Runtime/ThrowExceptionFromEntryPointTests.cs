// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Runtime
{
    public class ThrowExceptionFromEntryPointTests : BaseSystematicTest
    {
        public ThrowExceptionFromEntryPointTests(ITestOutputHelper output)
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

        [Fact(Timeout = 5000)]
        public void TestThrowExceptionFromEntryPoint()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                throw new InvalidOperationException();
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestThrowExceptionFromAsyncEntryPoint()
        {
            this.TestWithException<InvalidOperationException>(async r =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException();
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestThrowExceptionFromEntryPointWithActor()
        {
            this.TestWithException<InvalidOperationException>(r =>
            {
                ActorId m = r.CreateActor(typeof(M));
                throw new InvalidOperationException();
            },
            replay: true);
        }
    }
}
