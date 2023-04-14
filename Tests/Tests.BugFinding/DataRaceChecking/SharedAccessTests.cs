// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.DataRaceChecking
{
    public class SharedAccessTests : BaseBugFindingTest
    {
        public SharedAccessTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestSharedAccessRaceWithMemoryAccessInterleaving()
        {
            this.TestWithError(() =>
            {
                bool isSet = false;
                bool isDone = false;
                Thread t1 = new Thread(() =>
                {
                    isSet = true;
                    isDone = true;
                    isSet = false;
                });

                Thread t2 = new Thread(() =>
                {
                    if (isSet)
                    {
                        isDone = false;
                    }
                });

                t1.Start();
                t2.Start();
                t1.Join();
                t2.Join();

                Specification.Assert(isDone, "The expected condition was not satisfied.");
            },
            configuration: this.GetConfiguration()
                .WithMemoryAccessRaceCheckingEnabled()
                .WithTestingIterations(100),
            expectedError: "The expected condition was not satisfied.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSharedAccessRaceWithControlFlowInterleaving()
        {
            this.TestWithError(() =>
            {
                bool isSet = false;
                bool isDone = false;
                Thread t1 = new Thread(() =>
                {
                    isSet = true;
                    if (isSet)
                    {
                        isDone = true;
                        isSet = false;
                    }
                });

                Thread t2 = new Thread(() =>
                {
                    if (isSet)
                    {
                        isDone = false;
                    }
                });

                t1.Start();
                t2.Start();
                t1.Join();
                t2.Join();

                Specification.Assert(isDone, "The expected condition was not satisfied.");
            },
            configuration: this.GetConfiguration()
                .WithControlFlowRaceCheckingEnabled()
                .WithTestingIterations(100),
            expectedError: "The expected condition was not satisfied.",
            replay: true);
        }
    }
}
