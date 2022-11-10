// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Coyote.SystematicTesting;
using Xunit;
using Xunit.Abstractions;
using CoyoteTypes = Microsoft.Coyote.Rewriting.Types;

namespace Microsoft.Coyote.Runtime.Tests
{
    public class MemoryLeakTests : BaseRuntimeTest
    {
        public MemoryLeakTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestMemoryLeakInRuntime()
        {
            uint iterations = 1000;
            Configuration configuration = Configuration.Create()
                .WithTestingIterations(iterations)
                .WithTestIterationsRunToCompletion()
                .WithLockAccessRaceCheckingEnabled();

            long originalMemory = 0;
            long diffMemory = 0;
            GC.Collect(2, GCCollectionMode.Forced, true);

            this.RunSystematicTest(() =>
            {
                var semaphore1 = new SemaphoreSlim(1, 1);
                var semaphore2 = new SemaphoreSlim(1, 1);

                var t1 = CoyoteTypes.Threading.Tasks.Task.Run(() =>
                {
                    CoyoteTypes.Threading.SemaphoreSlim.Wait(semaphore1);
                    CoyoteTypes.Threading.SemaphoreSlim.Wait(semaphore2);
                    semaphore2.Release();
                    semaphore1.Release();
                });

                var t2 = CoyoteTypes.Threading.Tasks.Task.Run(() =>
                {
                    CoyoteTypes.Threading.SemaphoreSlim.Wait(semaphore2);
                    CoyoteTypes.Threading.SemaphoreSlim.Wait(semaphore1);
                    semaphore1.Release();
                    semaphore2.Release();
                });

                CoyoteTypes.Threading.Tasks.Task.WaitAll(t1, t2);
            },
            configuration,
            (iteration) =>
            {
                if (iteration > 0)
                {
                    diffMemory = GC.GetTotalMemory(false) - originalMemory;
                    if (diffMemory < 0)
                    {
                        diffMemory = 0;
                    }
                }
            },
            (iteration) =>
            {
                if (iteration == (uint)(iterations / 100))
                {
                    originalMemory = GC.GetTotalMemory(false);
                }

                GC.Collect(2, GCCollectionMode.Forced, true);
            });

            long threshold = originalMemory + (long)(originalMemory * 0.1);
            Assert.True(diffMemory <= threshold, string.Format(
                "Memory increase of {0} MB from {1} MB with threshold of {2} MB",
                diffMemory / 1024f / 1024f, originalMemory / 1024f / 1024f, threshold / 1024f / 1024f));
        }
    }
}
