// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Mono.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests.Exceptions
{
    public class ExceptionFilterRewritingTests : BaseRewritingTest
    {
        public ExceptionFilterRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionCanceledExceptionRethrow()
        {
            this.Test(() =>
            {
                try
                {
                    CheckCatchBlockRewriting(MethodBase.GetCurrentMethod(), 0);
                }
                catch (Exception)
                {
                    throw;
                }
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionCanceledExceptionExplicitRethrow()
        {
            this.Test(() =>
            {
                try
                {
                    CheckCatchBlockRewriting(MethodBase.GetCurrentMethod(), 0);
                }
                catch (Exception ex)
                {
#pragma warning disable CA2200 // Rethrow to preserve stack details.
                    throw ex;
#pragma warning restore CA2200 // Rethrow to preserve stack details.
                }
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionCanceledExceptionDoubleRethrow()
        {
            this.Test(() =>
            {
                try
                {
                    try
                    {
                        CheckCatchBlockRewriting(MethodBase.GetCurrentMethod(), 0);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionCanceledExceptionInEmptyCatchBlock()
        {
            this.Test(() =>
            {
                try
                {
                    CheckCatchBlockRewriting(MethodBase.GetCurrentMethod(), 1);
                }
                catch (Exception)
                {
                    // Needs rewriting to not consume.
                }
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionCanceledExceptionInDoubleEmptyCatchBlock()
        {
            this.Test(() =>
            {
                try
                {
                    try
                    {
                        CheckCatchBlockRewriting(MethodBase.GetCurrentMethod(), 2);
                    }
                    catch (Exception)
                    {
                        // Needs rewriting to not consume.
                    }
                }
                catch (Exception)
                {
                    // Needs rewriting to not consume.
                }
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionCanceledExceptionRethrowInEmptyCatchBlock()
        {
            this.Test(() =>
            {
                try
                {
                    try
                    {
                        CheckCatchBlockRewriting(MethodBase.GetCurrentMethod(), 1);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                catch (Exception)
                {
                    // Needs rewriting to not consume.
                }
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionCanceledExceptionInNonEmptyCatchBlock()
        {
            this.Test(() =>
            {
                CheckCatchBlockRewriting(MethodBase.GetCurrentMethod(), 1);

                try
                {
                    while (true)
                    {
                        Task.Delay(1).Wait();
                    }
                }
                catch (Exception ex)
                {
                    Specification.Assert(!(ex is ExecutionCanceledException), $"Must not catch '{typeof(ExecutionCanceledException)}'.");
                }
            },
            configuration: GetConfiguration().WithTestingIterations(1).WithMaxSchedulingSteps(10));
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionCanceledExceptionInNonEmptyCatchBlockAsync()
        {
            this.Test(async () =>
            {
                CheckCatchBlockRewriting(MethodBase.GetCurrentMethod(), 1);

                try
                {
                    while (true)
                    {
                        await Task.Delay(1);
                    }
                }
                catch (Exception ex)
                {
                    Specification.Assert(!(ex is ExecutionCanceledException), $"Must not catch '{typeof(ExecutionCanceledException)}'.");
                }
            },
            configuration: GetConfiguration().WithTestingIterations(1).WithMaxSchedulingSteps(10));
        }

        private static async Task<int> DelayAsync()
        {
            while (true)
            {
                await Task.Delay(1);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionCanceledExceptionInNonEmptyCatchBlockGenericAsync()
        {
            this.Test(async () =>
            {
                CheckCatchBlockRewriting(MethodBase.GetCurrentMethod(), 1);

                try
                {
                    while (true)
                    {
                        await DelayAsync();
                    }
                }
                catch (Exception ex)
                {
                    Specification.Assert(!(ex is ExecutionCanceledException), $"Must not catch '{typeof(ExecutionCanceledException)}'.");
                }
            },
            configuration: GetConfiguration().WithTestingIterations(1).WithMaxSchedulingSteps(10));
        }

        private static void CheckCatchBlockRewriting(MethodBase methodInfo, int expectedCount)
        {
            var instructions = methodInfo.GetInstructions();
            int count = instructions.Count(i => i.OpCode == OpCodes.Call &&
                i.Operand.ToString().Contains(nameof(ExceptionProvider.ThrowIfExecutionCanceledException)));
            Specification.Assert(count == expectedCount, $"Rewrote {count} catch blocks (expected {expectedCount}).");
        }
    }
}
