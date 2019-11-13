// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class BubbleSortAlgorithmTest : BaseTest
    {
        public BubbleSortAlgorithmTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class BubbleSortMachine : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                var rev = new List<int>();
                var sorted = new List<int>();

                for (int i = 0; i < 10; i++)
                {
                    rev.Insert(0, i);
                    sorted.Add(i);
                }

                this.Assert(rev.Count == 10);

                // Assert that simply reversing the list produces a sorted list.
                sorted = Reverse(rev);
                this.Assert(sorted.Count == 10);
                this.Assert(IsSorted(sorted));
                this.Assert(!IsSorted(rev));

                // Assert that the algorithm returns the sorted list.
                sorted = Sort(rev);
                this.Assert(sorted.Count == 10);
                this.Assert(IsSorted(sorted));
                this.Assert(!IsSorted(rev));
            }

            private static List<int> Reverse(List<int> l)
            {
                var result = l.ToList();

                int i = 0;
                int s = result.Count;
                while (i < s)
                {
                    int temp = result[i];
                    result.RemoveAt(i);
                    result.Insert(0, temp);
                    i = i + 1;
                }

                return result;
            }

            private static List<int> Sort(List<int> l)
            {
                var result = l.ToList();

                var swapped = true;
                while (swapped)
                {
                    int i = 0;
                    swapped = false;
                    while (i < result.Count - 1)
                    {
                        if (result[i] > result[i + 1])
                        {
                            int temp = result[i];
                            result[i] = result[i + 1];
                            result[i + 1] = temp;
                            swapped = true;
                        }

                        i = i + 1;
                    }
                }

                return result;
            }

            private static bool IsSorted(List<int> l)
            {
                int i = 0;
                while (i < l.Count - 1)
                {
                    if (l[i] > l[i + 1])
                    {
                        return false;
                    }

                    i = i + 1;
                }

                return true;
            }
        }

        [Fact(Timeout=10000)]
        public void TestBubbleSortAlgorithm()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(BubbleSortMachine));
            });
        }
    }
}
