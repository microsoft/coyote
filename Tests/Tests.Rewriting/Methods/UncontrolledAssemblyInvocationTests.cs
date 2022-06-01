// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class UncontrolledAssemblyInvocationTests : BaseRewritingTest
    {
        public UncontrolledAssemblyInvocationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsAwaiter()
        {
            this.Test(async () =>
            {
                await new Helpers.TaskAwaiter();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericAwaiter()
        {
            this.Test(async () =>
            {
                await new Helpers.GenericTaskAwaiter();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsAwaiterWithGenericArgument()
        {
            this.Test(async () =>
            {
                await new Helpers.TaskAwaiter<int>();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsTask()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsTaskFromGenericClass()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericTask()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetGenericTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsValueTupleTask()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetValueTupleTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericValueTupleTask()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetGenericValueTupleTask<int, bool>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericTaskFromGenericClass()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericArrayTaskFromGenericClass()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool[]>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericTaskFromGenericMethod()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetGenericMethodTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericValueTupleTaskFromGenericMethod()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetGenericValueTupleTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericTaskFromGenericClassLargeStack()
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

#if NET
        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsValueTask()
        {
            this.Test(async () =>
            {
                var task = ValueTaskProvider.GetTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsValueTaskFromGenericClass()
        {
            this.Test(async () =>
            {
                var task = GenericValueTaskProvider<object, bool>.Nested<short>.GetTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericValueTask()
        {
            this.Test(async () =>
            {
                var task = ValueTaskProvider.GetGenericTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericValueTaskFromGenericClass()
        {
            this.Test(async () =>
            {
                var task = GenericValueTaskProvider<object, bool>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericArrayValueTaskFromGenericClass()
        {
            this.Test(async () =>
            {
                var task = GenericValueTaskProvider<object, bool[]>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericValueTaskFromGenericMethod()
        {
            this.Test(async () =>
            {
                var task = GenericValueTaskProvider<object, bool>.Nested<short>.GetGenericMethodTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledMethodReturnsGenericValueTaskFromGenericClassLargeStack()
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
#endif
    }
}
