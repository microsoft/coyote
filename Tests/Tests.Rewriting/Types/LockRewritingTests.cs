// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;
using Monitor = System.Threading.Monitor;

#pragma warning disable CS9216 // Intentionally exercise the object's monitor independently of Lock.
namespace Microsoft.Coyote.Rewriting.Tests
{
    public class LockRewritingTests : BaseRewritingTest
    {
        public LockRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingLockMutualExclusion()
        {
            this.Test(() =>
            {
                var gate = new Lock();
                int entered = 0;
                Task first = Task.Run(() => Enter(gate, ref entered));
                Task second = Task.Run(() => Enter(gate, ref entered));
                Task.WaitAll(first, second);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestRewritingLockAndMonitorOwnershipIsIndependent(bool monitorHeld)
        {
            this.Test(() =>
            {
                var gate = new Lock();
                object monitor = gate;
                if (monitorHeld)
                {
                    Monitor.Enter(monitor);
                    try
                    {
                        Assert.False(gate.IsHeldByCurrentThread);
                        Assert.Throws<SynchronizationLockException>(() => gate.Exit());
                        Assert.True(gate.TryEnter());
                        gate.Exit();
                        Assert.False(gate.IsHeldByCurrentThread);
                        Assert.True(Monitor.IsEntered(monitor));
                    }
                    finally
                    {
                        Monitor.Exit(monitor);
                    }
                }
                else
                {
                    gate.Enter();
                    try
                    {
                        Assert.False(Monitor.IsEntered(monitor));
                        Assert.Throws<SynchronizationLockException>(() => Monitor.Exit(monitor));
                        Monitor.Enter(monitor);
                        Monitor.Exit(monitor);
                        Assert.False(Monitor.IsEntered(monitor));
                        Assert.True(gate.IsHeldByCurrentThread);
                    }
                    finally
                    {
                        gate.Exit();
                    }
                }
            });
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestRewritingLockAndMonitorCanBeHeldByDifferentOperations(bool monitorHeld)
        {
            this.Test(() =>
            {
                var gate = new Lock();
                object monitor = gate;
                if (monitorHeld)
                {
                    Monitor.Enter(monitor);
                    try
                    {
                        Task other = Task.Run(() =>
                        {
                            gate.Enter();
                            gate.Exit();
                        });
                        other.GetAwaiter().GetResult();
                    }
                    finally
                    {
                        Monitor.Exit(monitor);
                    }
                }
                else
                {
                    gate.Enter();
                    try
                    {
                        Task other = Task.Run(() =>
                        {
                            Monitor.Enter(monitor);
                            Monitor.Exit(monitor);
                        });
                        other.GetAwaiter().GetResult();
                    }
                    finally
                    {
                        gate.Exit();
                    }
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingLockLoweringUsesControlledEnterScope()
        {
            string assemblyPath = typeof(LockRewritingTests).Assembly.Location;
            string diff = File.ReadAllText(Path.ChangeExtension(assemblyPath, ".diff.json"));
            Assert.Contains(
                "System.Threading.Lock/Scope System.Threading.Lock::EnterScope()",
                diff);
            Assert.Contains(
                "Microsoft.Coyote.Rewriting.Types.Threading.Lock::EnterScope(System.Threading.Lock)",
                diff);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingNestedLockStatement()
        {
            this.Test(() =>
            {
                var gate = new Lock();
                lock (gate)
                {
                    Assert.True(gate.IsHeldByCurrentThread);
                    lock (gate)
                    {
                        Assert.True(gate.IsHeldByCurrentThread);
                    }

                    Assert.True(gate.IsHeldByCurrentThread);
                }

                Assert.False(gate.IsHeldByCurrentThread);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingLockScopeReleaseOnException()
        {
            this.Test(() =>
            {
                var gate = new Lock();
                Action throwInsideLock = () =>
                {
                    lock (gate)
                    {
                        throw new InvalidOperationException();
                    }
                };
                Assert.Throws<InvalidOperationException>(throwInsideLock);

                Assert.False(gate.IsHeldByCurrentThread);
                gate.Enter();
                gate.Exit();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingDefaultAndCopiedLockScope()
        {
            this.Test(() =>
            {
                var gate = new Lock();
                Lock.Scope emptyScope = default;
                emptyScope.Dispose();

                Lock.Scope scope = gate.EnterScope();
                Lock.Scope copiedScope = scope;
                Assert.True(gate.IsHeldByCurrentThread);
                scope.Dispose();
                Assert.False(gate.IsHeldByCurrentThread);
                try
                {
                    copiedScope.Dispose();
                    Assert.True(false, "Disposing a copied scope should fail after the original scope exits.");
                }
                catch (SynchronizationLockException)
                {
                }
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingLockTryEnterVariants()
        {
            this.Test(() =>
            {
                var gate = new Lock();
                Assert.True(gate.TryEnter());
                Assert.True(gate.TryEnter(0));
                Assert.True(gate.TryEnter(Timeout.Infinite));
                Assert.True(gate.TryEnter(TimeSpan.Zero));
                Assert.True(gate.TryEnter(Timeout.InfiniteTimeSpan));
                Assert.True(gate.IsHeldByCurrentThread);
                gate.Exit();
                gate.Exit();
                gate.Exit();
                gate.Exit();
                gate.Exit();

                Assert.Throws<ArgumentOutOfRangeException>(() => gate.TryEnter(-2));
                Assert.Throws<ArgumentOutOfRangeException>(() => gate.TryEnter(TimeSpan.FromMilliseconds(-2)));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingLockFiniteTimeoutIsExplicitlyUnsupported()
        {
            this.TestWithError(() =>
            {
                var gate = new Lock();
                Task owner = Task.Run(() =>
                {
                    gate.Enter();
                    SchedulingPoint.Interleave();
                    gate.Exit();
                });

                Task contender = Task.Run(() =>
                {
                    SchedulingPoint.Interleave();
                    if (gate.TryEnter(1))
                    {
                        gate.Exit();
                    }
                });

                Task.WaitAll(owner, contender);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Invoking 'Lock.TryEnter' with a finite timeout is not supported in systematic testing.");
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingLockTryEnterWithLockAccessRaceChecking()
        {
            this.Test(() =>
            {
                var gate = new Lock();
                int entered = 0;
                Task first = Task.Run(() => TryEnter(gate, ref entered));
                Task second = Task.Run(() => TryEnter(gate, ref entered));
                Task.WaitAll(first, second);
            },
            configuration: this.GetConfiguration().WithLockAccessRaceCheckingEnabled().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingLockContentionExploresBothAcquisitionOrders()
        {
            var winners = new HashSet<int>();
            this.Test(() =>
            {
                var gate = new Lock();
                int winner = 0;
                Task first = Task.Run(() =>
                {
                    lock (gate)
                    {
                        if (winner is 0)
                        {
                            winner = 1;
                        }

                        SchedulingPoint.Interleave();
                    }
                });

                Task second = Task.Run(() =>
                {
                    lock (gate)
                    {
                        if (winner is 0)
                        {
                            winner = 2;
                        }

                        SchedulingPoint.Interleave();
                    }
                });

                Task.WaitAll(first, second);
                lock (winners)
                {
                    winners.Add(winner);
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));

            Assert.Contains(1, winners);
            Assert.Contains(2, winners);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingLockDeadlockDetection()
        {
            this.TestWithError(() =>
            {
                var firstGate = new Lock();
                var secondGate = new Lock();
                Task first = Task.Run(() =>
                {
                    lock (firstGate)
                    {
                        SchedulingPoint.Interleave();
                        lock (secondGate)
                        {
                        }
                    }
                });

                Task second = Task.Run(() =>
                {
                    lock (secondGate)
                    {
                        SchedulingPoint.Interleave();
                        lock (firstGate)
                        {
                        }
                    }
                });

                Task.WaitAll(first, second);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) => Assert.StartsWith("Deadlock detected.", e));
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingLockNonOwnerExit()
        {
            this.TestWithException<SynchronizationLockException>(async () =>
            {
                var gate = new Lock();
                gate.Enter();
                await Task.Run(() => gate.Exit());
            });
        }

        private static void Enter(Lock gate, ref int entered)
        {
            lock (gate)
            {
                entered++;
                Specification.Assert(entered is 1, "More than one operation entered the lock.");
                SchedulingPoint.Interleave();
                entered--;
            }
        }

        private static void TryEnter(Lock gate, ref int entered)
        {
            while (!gate.TryEnter())
            {
                SchedulingPoint.Interleave();
            }

            entered++;
            Specification.Assert(entered is 1, "More than one operation entered the lock.");
            SchedulingPoint.Interleave();
            entered--;
            gate.Exit();
        }
    }
}
#pragma warning restore CS9216 // Intentionally exercise the object's monitor independently of Lock.
#endif
