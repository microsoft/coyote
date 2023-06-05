// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Coyote.Rewriting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class UncontrolledValueTaskTests : BaseBugFindingTest
    {
        public UncontrolledValueTaskTests(ITestOutputHelper output)
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
        private class UncontrolledValueTaskAwaiter
        {
#if NET
#pragma warning disable CA1822 // Mark members as static
            public ValueTaskAwaiter GetAwaiter() => ValueTask.CompletedTask.GetAwaiter();
#pragma warning restore CA1822 // Mark members as static
#endif
        }

        /// <summary>
        /// Helper class for task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private class UncontrolledGenericValueTaskAwaiter
        {
#if NET
#pragma warning disable CA1822 // Mark members as static
            public ValueTaskAwaiter<int> GetAwaiter() => ValueTask.FromResult<int>(0).GetAwaiter();
#pragma warning restore CA1822 // Mark members as static
#endif
        }

        /// <summary>
        /// Helper class for task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private class UncontrolledValueTaskAwaiter<T>
        {
#if NET
            public ValueTaskAwaiter<T> GetAwaiter() => ValueTask.FromResult<T>(default).GetAwaiter();
#endif
        }

#if NET
        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledValueTaskAwaiter()
        {
            this.Test(async () =>
            {
                await new UncontrolledValueTaskAwaiter();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledGenericValueTaskAwaiter()
        {
            this.Test(async () =>
            {
                await new UncontrolledGenericValueTaskAwaiter();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledValueTaskAwaiterWithGenericArgument()
        {
            this.Test(async () =>
            {
                await new UncontrolledValueTaskAwaiter<int>();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }
#endif
    }
}
