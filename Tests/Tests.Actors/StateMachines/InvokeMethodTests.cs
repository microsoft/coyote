// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.UnitTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests.StateMachines
{
    public class InvokeMethodTests : BaseActorTest
    {
        public InvokeMethodTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M1 : Actor
        {
            internal int Add(int m, int k)
            {
                return m + k;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInvokeInternalMethod()
        {
            var configuration = this.GetConfiguration();
            var test = new ActorTestKit<M1>(configuration: configuration);

            int result = test.ActorInstance.Add(3, 4);
            test.Assert(result == 7, $"Incorrect result '{result}'.");
        }

        private class M2 : Actor
        {
            internal async Task<int> AddAsync(int m, int k)
            {
                await Task.CompletedTask;
                return m + k;
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestInvokeInternalAsyncMethod()
        {
            var configuration = this.GetConfiguration();
            var test = new ActorTestKit<M2>(configuration: configuration);

            int result = await test.ActorInstance.AddAsync(3, 4);
            test.Assert(result == 7, $"Incorrect result '{result}'.");
        }

        private class M3 : Actor
        {
            private int Add(int m, int k)
            {
                return m + k;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInvokePrivateMethod()
        {
            var configuration = this.GetConfiguration();
            var test = new ActorTestKit<M3>(configuration: configuration);

            int result = (int)test.Invoke("Add", 3, 4);
            test.Assert(result == 7, $"Incorrect result '{result}'.");

            result = (int)test.Invoke("Add", new Type[] { typeof(int), typeof(int) }, 3, 4);
            test.Assert(result == 7, $"Incorrect result '{result}'.");
        }

        private class M4 : Actor
        {
            private async Task<int> AddAsync(int m, int k)
            {
                await Task.CompletedTask;
                return m + k;
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestInvokePrivateAsyncMethod()
        {
            var configuration = this.GetConfiguration();
            var test = new ActorTestKit<M4>(configuration: configuration);

            int result = (int)await test.InvokeAsync("AddAsync", 3, 4);
            test.Assert(result == 7, $"Incorrect result '{result}'.");

            result = (int)await test.InvokeAsync("AddAsync", new Type[] { typeof(int), typeof(int) }, 3, 4);
            test.Assert(result == 7, $"Incorrect result '{result}'.");
        }
    }
}
