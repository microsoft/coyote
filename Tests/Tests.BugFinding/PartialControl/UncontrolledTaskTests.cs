// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Coyote.Rewriting;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class UncontrolledTaskTests : BaseBugFindingTest
    {
        public UncontrolledTaskTests(ITestOutputHelper output)
            : base(output)
        {
        }

        /// <summary>
        /// Helper class for task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private class UncontrolledTaskAwaiter
        {
#pragma warning disable CA1822 // Mark members as static
            public TaskAwaiter GetAwaiter() => Task.CompletedTask.GetAwaiter();
#pragma warning restore CA1822 // Mark members as static
        }

        /// <summary>
        /// Helper class for task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private class UncontrolledGenericTaskAwaiter
        {
#pragma warning disable CA1822 // Mark members as static
            public TaskAwaiter<int> GetAwaiter() => Task.FromResult<int>(0).GetAwaiter();
#pragma warning restore CA1822 // Mark members as static
        }

        /// <summary>
        /// Helper class for task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private class UncontrolledTaskAwaiter<T>
        {
            public TaskAwaiter<T> GetAwaiter() => Task.FromResult<T>(default).GetAwaiter();
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledDelay()
        {
            this.Test(async () =>
            {
                await AsyncProvider.DelayAsync(100);
            },
            configuration: this.GetConfiguration()
                .WithPartiallyControlledConcurrencyAllowed()
                .WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledDelayWithNoPartialControl()
        {
            this.TestWithError(async () =>
            {
                await AsyncProvider.DelayAsync(100);
            },
            configuration: this.GetConfiguration().WithTestingIterations(10),
            errorChecker: (e) =>
            {
                var expectedMethodName = GetFullyQualifiedMethodName(typeof(AsyncProvider), nameof(AsyncProvider.DelayAsync));
                Assert.StartsWith($"Invoking '{expectedMethodName}' returned task", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledTaskAwaiter()
        {
            this.Test(async () =>
            {
                await new UncontrolledTaskAwaiter();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledGenericTaskAwaiter()
        {
            this.Test(async () =>
            {
                await new UncontrolledGenericTaskAwaiter();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledTaskAwaiterWithGenericArgument()
        {
            this.Test(async () =>
            {
                await new UncontrolledTaskAwaiter<int>();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }
    }
}
