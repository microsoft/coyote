// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class ConstructorRewritingTests : BaseRewritingTest
    {
        public ConstructorRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "This test covers the rewriting of Task-using code. The test does not actually execute the tasks as a normal xUnit test would, so the flagged operation is intentional.")]
        [Fact(Timeout = 5000)]
        public void TestRewritingGenericClassConstructor()
        {
            var box = new Box<TaskAwaiter>(Task.CompletedTask.GetAwaiter());
            Assert.NotNull(box);
            Assert.True(box.Value.IsCompleted);
            box.Value.GetResult();
        }

        [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "This test covers the rewriting of Task-using code. The test does not actually execute the tasks as a normal xUnit test would, so the flagged operation is intentional.")]
        [Fact(Timeout = 5000)]
        public void TestRewritingGenericStructConstructor()
        {
            var value = CreateValue(Task.CompletedTask.GetAwaiter());
            Assert.True(value.Value.IsCompleted);
            value.Value.GetResult();
        }

        [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "This test covers the rewriting of Task-using code. The test does not actually execute the tasks as a normal xUnit test would, so the flagged operation is intentional.")]
        [Fact(Timeout = 5000)]
        public void TestRewritingGenericDelegateConstructor()
        {
            Func<TaskAwaiter> getAwaiter = GetAwaiter;
            var awaiter = getAwaiter();
            Assert.True(awaiter.IsCompleted);
            awaiter.GetResult();
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingConstructorToStaticFactory()
        {
            using var semaphore = new SemaphoreSlim(1, 2);
            Assert.Equal(1, semaphore.CurrentCount);
            Assert.True(semaphore.Wait(0));
            Assert.Equal(0, semaphore.CurrentCount);
            Assert.Equal(0, semaphore.Release());
        }

        private static TaskAwaiter GetAwaiter() => Task.CompletedTask.GetAwaiter();

        private static ValueBox<TaskAwaiter> CreateValue(TaskAwaiter value) =>
            new ValueBox<TaskAwaiter>(value);

        private sealed class Box<T>
        {
            internal T Value { get; }

            internal Box(T value)
            {
                this.Value = value;
            }
        }

        private readonly struct ValueBox<T>
        {
            internal T Value { get; }

            internal ValueBox(T value)
            {
                this.Value = value;
            }
        }
    }
}
