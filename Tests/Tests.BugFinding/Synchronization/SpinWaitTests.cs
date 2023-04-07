// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;
using Xunit.Abstractions;
using Interlocked = System.Threading.Interlocked;
using SpinWait = System.Threading.SpinWait;
using Thread = System.Threading.Thread;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class SpinWaitTests : BaseBugFindingTest
    {
        public SpinWaitTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestSpinCount()
        {
            this.Test(() =>
            {
                SpinWait spinner = new SpinWait();
                Assert.Equal(0, spinner.Count);

                spinner.SpinOnce(sleep1Threshold: -1);
                Assert.Equal(1, spinner.Count);
                spinner.SpinOnce(sleep1Threshold: 0);
                Assert.Equal(2, spinner.Count);
                spinner.SpinOnce(sleep1Threshold: 1);
                Assert.Equal(3, spinner.Count);
                spinner.SpinOnce(sleep1Threshold: int.MaxValue);
                Assert.Equal(4, spinner.Count);
                int i = 5;
                for (; i < 10; ++i)
                {
                    spinner.SpinOnce(sleep1Threshold: -1);
                    Assert.Equal(i, spinner.Count);
                }

                for (; i < 20; ++i)
                {
                    spinner.SpinOnce(sleep1Threshold: 15);
                    Assert.Equal(i, spinner.Count);
                }
            }, configuration: this.GetConfiguration());
        }

        [Fact(Timeout = 5000)]
        public void TestSpinUntil()
        {
            this.Test(() =>
            {
                SpinWait.SpinUntil(() => true);
                Assert.True(SpinWait.SpinUntil(() => true, 0), "Assertion failed.");
            }, configuration: this.GetConfiguration());
        }

        [Fact(Timeout = 5000)]
        public void TestSpinUntilMultithreaded()
        {
            this.Test(() =>
            {
                int counter = 0;
                Thread t1 = new Thread(() =>
                {
                    SpinWait spin = default(SpinWait);
                    for (int i = 0; i < 5; i++)
                    {
                        counter++;
                        spin.SpinOnce();
                    }
                });

                Thread t2 = new Thread(() =>
                {
                    SpinWait.SpinUntil(() => counter == 5);
                    counter = 0;
                });

                t1.Start();
                t2.Start();
                t1.Join();
                t2.Join();

                Assert.Equal(counter, 0);
            }, configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestSpinUntilDeadlock()
        {
            this.TestWithError(() =>
            {
                SpinWait.SpinUntil(() => false);
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        public class LockFreeStack<T>
        {
            private class Node
            {
                public Node Next;
                public T Value;
            }

            private volatile Node Head;

            public void Push(T item)
            {
                SpinWait spin = default(SpinWait);
                Node node = new Node { Value = item }, head;
                while (true)
                {
                    head = this.Head;
                    node.Next = head;
                    if (Interlocked.CompareExchange(ref this.Head, node, head) == head)
                    {
                        break;
                    }

                    spin.SpinOnce();
                }
            }

            public bool TryPop(out T result)
            {
                result = default(T);

                SpinWait spin = default(SpinWait);
                Node head;
                while (true)
                {
                    head = this.Head;
                    if (head == null)
                    {
                        return false;
                    }

                    if (Interlocked.CompareExchange(ref this.Head, head.Next, head) == head)
                    {
                        result = head.Value;
                        return true;
                    }

                    spin.SpinOnce();
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSpinWaitStack()
        {
            var configuration = this.GetConfiguration().WithTestingIterations(100)
                .WithAtomicOperationRaceCheckingEnabled(true);
            this.Test(() =>
            {
                var stack = new LockFreeStack<int>();
                Thread t1 = new Thread(() =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        stack.Push(i);
                    }
                });

                Thread t2 = new Thread(() =>
                {
                    int count = 5;
                    while (count > 0)
                    {
                        if (stack.TryPop(out int value))
                        {
                            count--;
                        }
                        else
                        {
                            Thread.Yield();
                        }
                    }
                });

                t1.Start();
                t2.Start();
                t1.Join();
                t2.Join();

                Assert.False(stack.TryPop(out int _), "Stack is not empty.");
            }, configuration: configuration);
        }
    }
}
