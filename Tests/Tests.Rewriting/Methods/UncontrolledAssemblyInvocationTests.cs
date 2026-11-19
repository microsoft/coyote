// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
#if NET
using System.Threading.Channels;
#endif
using System.Threading.Tasks;
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

#pragma warning disable CA1000 // Do not declare static members on generic types
        /// <summary>
        /// Helper class for task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private static class TaskProvider
        {
            public static Task GetTask() => Task.CompletedTask;

            public static Task<T> GetGenericTask<T>() => Task.FromResult<T>(default(T));

            public static Task<T[]> GetGenericTaskArray<T>() => Task.FromResult<T[]>(default(T[]));

            public static Task<(int, bool)> GetValueTupleTask() => Task.FromResult((0, true));

            public static Task<(TLeft, TRight)> GetGenericValueTupleTask<TLeft, TRight>() =>
                Task.FromResult((default(TLeft), default(TRight)));

            public static Task<(T, (TLeft, TRight))> GetGenericNestedValueTupleTask<T, TLeft, TRight>() =>
                Task.FromResult((default(T), (default(TLeft), default(TRight))));
        }

        /// <summary>
        /// Helper class for task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private static class GenericTaskProvider<TLeft, TRight>
        {
            public static class Nested<TNested>
            {
                public static Task GetTask() => Task.CompletedTask;

                public static Task<T> GetGenericMethodTask<T>() => Task.FromResult<T>(default(T));

                public static Task<TRight> GetGenericTypeTask<T>() => Task.FromResult<TRight>(default(TRight));

                public static Task<(T, TRight, TNested)> GetGenericValueTupleTask<T>() =>
                    Task.FromResult((default(T), default(TRight), default(TNested)));
            }
        }

#if NET
        /// <summary>
        /// Helper class for value task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private static class ValueTaskProvider
        {
            public static ValueTask GetTask() => ValueTask.CompletedTask;

            public static ValueTask<T> GetGenericTask<T>() => ValueTask.FromResult<T>(default(T));

            public static ValueTask<T[]> GetGenericTaskArray<T>() => ValueTask.FromResult<T[]>(default(T[]));
        }

        /// <summary>
        /// Helper class for value task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private static class GenericValueTaskProvider<TLeft, TRight>
        {
            public static class Nested<TNested>
            {
                public static ValueTask GetTask() => ValueTask.CompletedTask;

                public static ValueTask<T> GetGenericMethodTask<T>() => ValueTask.FromResult<T>(default(T));

                public static ValueTask<TRight> GetGenericTypeTask<T>() => ValueTask.FromResult<TRight>(default(TRight));
            }
        }
#endif
#pragma warning restore CA1000 // Do not declare static members on generic types

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
        public void TestUncontrolledMethodReturnsGenericTaskArray()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetGenericTaskArray<int>();
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
        public void TestUncontrolledMethodReturnsGenericNestedValueTupleTask()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetGenericNestedValueTupleTask<int, bool, short>();
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
        public void TestUncontrolledValueTaskReferences()
        {
            this.Test(() =>
            {
                var provider = new UncontrolledValueTaskReferences<int>();
                ref ValueTask task = ref provider.GetTask();
                task = ValueTask.FromException(new InvalidOperationException());
                Assert.True(provider.GetTask().IsFaulted);
                ref ValueTask<int> genericTask = ref provider.GetGenericTask();
                genericTask = new ValueTask<int>(42);
                Assert.Equal(42, provider.GetGenericTask().Result);
                Assert.Equal(42, provider.GetReadOnlyTask().Result);
                ForwardValueTask(provider) = new ValueTask<int>(123);
                Assert.Equal(123, provider.GetGenericTask().Result);
                provider.GetMethodTask<long>() = new ValueTask<long>(456);
                Assert.Equal(456, provider.GetMethodTask<long>().Result);
            });
        }

        private static ref ValueTask<int> ForwardValueTask(UncontrolledValueTaskReferences<int> provider) =>
            ref provider.GetGenericTask();

        [Fact(Timeout = 5000)]
        public void TestControlledTaskReturnedByReference()
        {
            this.Test(async () =>
            {
                var completion = new TaskCompletionSource<int>();
                var provider = new UncontrolledValueTaskReferences<int>();
                provider.SetGenericTask(new ValueTask<int>(completion.Task));
                ValueTask<int> task = provider.GetGenericTask();
                Task producer = Task.Run(() => completion.SetResult(42));
                Assert.Equal(42, await task);
                await producer;
            },
            configuration: this.GetConfiguration().WithTestingIterations(20)
                .WithPartiallyControlledConcurrencyAllowed(false).WithSystematicFuzzingFallbackEnabled(false));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void TestUncontrolledPendingValueTask(bool isGeneric, bool isReference)
        {
            this.TestWithError(() =>
            {
                var provider = new UncontrolledValueTaskReferences<int>();
                provider.SetPendingTasks();
                if (isGeneric)
                {
                    _ = isReference ? provider.GetGenericTask() : provider.GetGenericTaskValue();
                }
                else
                {
                    _ = isReference ? provider.GetTask() : provider.GetTaskValue();
                }
            },
            errorChecker: (error) =>
            {
                Assert.Contains("returned task", error);
                Assert.Contains("not intercepted and controlled", error);
            },
            configuration: this.GetConfiguration().WithPartiallyControlledConcurrencyAllowed(false)
                .WithSystematicFuzzingFallbackEnabled(false));
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledValueTaskStoredOrReturned()
        {
            this.Test(async () =>
            {
                var buffer = new byte[1];
                ValueTask<int> read = new MemoryStream(new byte[] { 42 }).ReadAsync(buffer.AsMemory());
                await Task.Yield();
                Assert.Equal(1, await read);
                Assert.Equal(42, buffer[0]);

                await ReturnValueTask();
                Assert.Equal(1, await ReturnGenericValueTask());

                var channel = Channel.CreateUnbounded<int>();
                Assert.True(channel.Writer.TryWrite(123));
                Assert.Equal(123, await ReadChannel(channel.Reader));
            });
        }

        private static ValueTask ReturnValueTask() => new MemoryStream().DisposeAsync();

        private static ValueTask<int> ReturnGenericValueTask() =>
            new MemoryStream(new byte[] { 1 }).ReadAsync(new byte[1].AsMemory());

        private static ValueTask<T> ReadChannel<T>(ChannelReader<T> reader) => reader.ReadAsync();

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
        public void TestUncontrolledMethodReturnsGenericValueTaskArray()
        {
            this.Test(async () =>
            {
                var task = ValueTaskProvider.GetGenericTaskArray<int>();
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
