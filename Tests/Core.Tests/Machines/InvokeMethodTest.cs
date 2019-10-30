// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.TestingServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class InvokeMethodTest : BaseTest
    {
        public InvokeMethodTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M1 : StateMachine
        {
            [Start]
            private class Init : MachineState
            {
            }

            internal int Add(int m, int k)
            {
                return m + k;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInvokeInternalMethod()
        {
            var configuration = GetConfiguration();
            var test = new StateMachineTestKit<M1>(configuration: configuration);

            int result = test.Machine.Add(3, 4);
            test.Assert(result == 7, $"Incorrect result '{result}'");
        }

        private class M2 : StateMachine
        {
            [Start]
            private class Init : MachineState
            {
            }

            internal async Task<int> AddAsync(int m, int k)
            {
                await Task.CompletedTask;
                return m + k;
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestInvokeInternalAsyncMethod()
        {
            var configuration = GetConfiguration();
            var test = new StateMachineTestKit<M2>(configuration: configuration);

            int result = await test.Machine.AddAsync(3, 4);
            test.Assert(result == 7, $"Incorrect result '{result}'");
        }

        private class M3 : StateMachine
        {
            [Start]
            private class Init : MachineState
            {
            }

#pragma warning disable IDE0051 // Remove unused private members
            private int Add(int m, int k)
            {
                return m + k;
            }
#pragma warning restore IDE0051 // Remove unused private members
        }

        [Fact(Timeout = 5000)]
        public void TestInvokePrivateMethod()
        {
            var configuration = GetConfiguration();
            var test = new StateMachineTestKit<M3>(configuration: configuration);

            int result = (int)test.Invoke("Add", 3, 4);
            test.Assert(result == 7, $"Incorrect result '{result}'");

            result = (int)test.Invoke("Add", new Type[] { typeof(int), typeof(int) }, 3, 4);
            test.Assert(result == 7, $"Incorrect result '{result}'");
        }

        private class M4 : StateMachine
        {
            [Start]
            private class Init : MachineState
            {
            }

#pragma warning disable IDE0051 // Remove unused private members
            private async Task<int> AddAsync(int m, int k)
            {
                await Task.CompletedTask;
                return m + k;
            }
#pragma warning restore IDE0051 // Remove unused private members
        }

        [Fact(Timeout = 5000)]
        public async Task TestInvokePrivateAsyncMethod()
        {
            var configuration = GetConfiguration();
            var test = new StateMachineTestKit<M4>(configuration: configuration);

            int result = (int)await test.InvokeAsync("AddAsync", 3, 4);
            test.Assert(result == 7, $"Incorrect result '{result}'");

            result = (int)await test.InvokeAsync("AddAsync", new Type[] { typeof(int), typeof(int) }, 3, 4);
            test.Assert(result == 7, $"Incorrect result '{result}'");
        }
    }
}
