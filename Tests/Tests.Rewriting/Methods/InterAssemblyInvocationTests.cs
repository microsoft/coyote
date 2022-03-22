// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class InterAssemblyInvocationTests : BaseRewritingTest
    {
        public InterAssemblyInvocationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithAwaiter()
        {
            this.Test(async () =>
            {
                await new Helpers.TaskAwaiter();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithGenericAwaiter()
        {
            this.Test(async () =>
            {
                await new Helpers.GenericTaskAwaiter();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithAwaiterWithGenericArgument()
        {
            this.Test(async () =>
            {
                await new Helpers.TaskAwaiter<int>();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithReturnedTask()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithReturnedGenericTask()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetGenericTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithReturnedNestedGenericTask()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithReturnedNestedGenericMethodTask()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetGenericMethodTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithReturnedNestedGenericTypeTask()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithReturnedNestedGenericTypeTaskLargeStack()
        {
            this.Test(async () =>
            {
                var obj1 = new object();
                var obj2 = new object();
                var obj3 = new object();
                var obj4 = new object();
                var obj5 = new object();
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithReturnedValueTask()
        {
            this.Test(async () =>
            {
                var task = ValueTaskProvider.GetTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithReturnedGenericValueTask()
        {
            this.Test(async () =>
            {
                var task = ValueTaskProvider.GetGenericTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithReturnedNestedGenericValueTask()
        {
            this.Test(async () =>
            {
                var task = GenericValueTaskProvider<object, bool>.Nested<short>.GetTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithReturnedNestedGenericMethodValueTask()
        {
            this.Test(async () =>
            {
                var task = GenericValueTaskProvider<object, bool>.Nested<short>.GetGenericMethodTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithReturnedNestedGenericTypeValueTask()
        {
            this.Test(async () =>
            {
                var task = GenericValueTaskProvider<object, bool>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterAssemblyInvocationWithReturnedNestedGenericTypeValueTaskLargeStack()
        {
            this.Test(async () =>
            {
                var obj1 = new object();
                var obj2 = new object();
                var obj3 = new object();
                var obj4 = new object();
                var obj5 = new object();
                var task = GenericValueTaskProvider<object, bool>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }
    }
}
