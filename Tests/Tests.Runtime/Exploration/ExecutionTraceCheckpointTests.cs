// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using CoyoteTypes = Microsoft.Coyote.Rewriting.Types;

namespace Microsoft.Coyote.Runtime.Tests
{
    public class ExecutionTraceCheckpointTests : BaseRuntimeTest
    {
        public ExecutionTraceCheckpointTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static bool IsSequenceFound(List<int> values)
        {
            for (int i = 0; i < values.Count && i < values.Capacity / 2; i++)
            {
                if (values[i] != 0)
                {
                    return false;
                }
            }

            for (int i = 5; i < values.Count && i < values.Capacity; i++)
            {
                if (values[i] != 1)
                {
                    return false;
                }
            }

            return true;
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceCheckpoint()
        {
            uint numSequencesFound = 0;
            bool isSnapshotReset = true;
            this.RunSystematicTest(() =>
            {
                var values = new List<int>(10);
                var task1 = CoyoteTypes.Threading.Tasks.Task.Run(() =>
                {
                    while (values.Count < values.Capacity)
                    {
                        values.Add(0);
                        if (IsSequenceFound(values))
                        {
                            Microsoft.Coyote.Runtime.SchedulingPoint.SetCheckpoint();
                        }

                        Microsoft.Coyote.Runtime.SchedulingPoint.Interleave();
                    }
                });

                var task2 = CoyoteTypes.Threading.Tasks.Task.Run(() =>
                {
                    while (values.Count < values.Capacity)
                    {
                        values.Add(1);
                        if (IsSequenceFound(values))
                        {
                            Microsoft.Coyote.Runtime.SchedulingPoint.SetCheckpoint();
                        }

                        Microsoft.Coyote.Runtime.SchedulingPoint.Interleave();
                    }
                });

                CoyoteTypes.Threading.Tasks.Task.WaitAll(task1, task2);
                Assert.True(values.Count == values.Capacity);
                this.TestOutput.WriteLine("Values: {0}", string.Join(string.Empty, values));

                bool isSequenceFound = true;
                for (int i = 0; i < values.Capacity / 2; i++)
                {
                    if (values[i] != 0)
                    {
                        isSequenceFound = false;
                    }
                }

                for (int i = 5; i < values.Capacity; i++)
                {
                    if (values[i] != 1)
                    {
                        isSequenceFound = false;
                    }
                }

                if (isSequenceFound)
                {
                    // Count the number of found sequences. We snapshot the prefix
                    // so we expect this to be a very high value.
                    numSequencesFound++;
                    if (numSequencesFound is 1)
                    {
                        isSnapshotReset = false;
                    }
                }

                if (numSequencesFound > 0)
                {
                    // Store a value that helps ensure that once the first sequence
                    // is found, the snapshot is never reset and the sequence can be
                    // always found in all subsequent iterations.
                    isSnapshotReset &= isSequenceFound;
                }
            },
            configuration: this.GetConfiguration()
                .WithTestingIterations(100)
                .WithTestIterationsRunToCompletion());
            Assert.True(numSequencesFound > 50, $"Expected at least '50' sequences, but found '{numSequencesFound}'.");
            Assert.False(isSnapshotReset, "Snapshot was reset.");
        }
    }
}
