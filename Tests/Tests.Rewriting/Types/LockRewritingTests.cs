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
using Microsoft.Coyote.Tests.Common.Threading;
using Xunit;
using Xunit.Abstractions;
using Monitor = System.Threading.Monitor;

#pragma warning disable CS9216 // Intentionally exercise the object's monitor independently of Lock.
namespace Microsoft.Coyote.Rewriting.Tests
{
    public class LockRewritingTests : BaseRewritingTest
    {
        private delegate Lock.Scope EnterScopeDelegate();

        public LockRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory(Timeout = 5000)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void TestRewritingExternalLockScopeIsRejectedBeforeInvocation(bool allowPartialControl, bool isInstanceCall)
        {
            bool invoked = false;
            this.TestWithError(() =>
            {
                var gate = new Lock();
                using var scope = isInstanceCall ?
                    new LockScopeProvider().EnterInstance(gate, () => invoked = true) :
                    LockScopeProvider.Enter(gate, () => invoked = true);
            },
            configuration: this.GetConfiguration()
                .WithPartiallyControlledConcurrencyAllowed(allowPartialControl)
                .WithSystematicFuzzingFallbackEnabled(false),
            errorChecker: (e) =>
            {
                Assert.Contains("LockScopeProvider.Enter", e);
                Assert.Contains("uncontrolled lock scope", e);
            });
            Assert.False(invoked);
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestRewritingExternalLockScopeFallsBackToFuzzing(bool allowPartialControl)
        {
            int completed = 0;
            this.Test(() =>
            {
                var gate = new Lock();
                using (LockScopeProvider.Enter(gate, () =>
                {
                    Assert.Equal(SchedulingPolicy.Fuzzing, CoyoteRuntime.Current.SchedulingPolicy);
                }))
                {
                    Assert.True(gate.IsHeldByCurrentThread);
                    gate.Enter();
                    gate.Exit();
                }

                Assert.False(gate.IsHeldByCurrentThread);
                completed++;
            },
            configuration: this.GetConfiguration().WithPartiallyControlledConcurrencyAllowed(allowPartialControl)
                .WithSystematicFuzzingFallbackEnabled()
                .WithTestingIterations(3));
            Assert.True(completed > 0);
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestRewritingExternalLockScopePreservesNativeDisposal(bool isInstanceCall)
        {
            var gate = new Lock();
            bool invoked = false;
            Lock.Scope scope = isInstanceCall ?
                new LockScopeProvider().EnterInstance(gate, () => invoked = true) :
                LockScopeProvider.Enter(gate, () => invoked = true);
            Lock.Scope copy = scope;
            Assert.True(invoked);
            Assert.True(gate.IsHeldByCurrentThread);
            scope.Dispose();
            scope.Dispose();
            Assert.False(gate.IsHeldByCurrentThread);
            try
            {
                copy.Dispose();
                Assert.Fail("Disposing a copied native scope should fail after the original scope exits.");
            }
            catch (SynchronizationLockException)
            {
            }

            Action throwInsideScope = () =>
            {
                using var anotherScope = LockScopeProvider.Enter(gate, () => { });
                Assert.True(gate.IsHeldByCurrentThread);
                throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(throwInsideScope);
            Assert.False(gate.IsHeldByCurrentThread);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingExternalLockScopeOnUnusedPathDoesNotFail()
        {
            bool useExternal = false;
            this.Test(() =>
            {
                var gate = new Lock();
                using (useExternal ? LockScopeProvider.Enter(gate, () => Assert.Fail()) : gate.EnterScope())
                {
                    Assert.True(gate.IsHeldByCurrentThread);
                    Assert.False(Monitor.IsEntered(gate));
                }

                Assert.False(gate.IsHeldByCurrentThread);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingConstrainedExternalLockScopePreservesNativeDisposal()
        {
            var gate = new Lock();
            LockScopeProvider.ValueProvider provider = default;
            bool invoked = false;
            UseExternalLockScope(ref provider, gate, () =>
            {
                invoked = true;
                Assert.True(gate.IsHeldByCurrentThread);
            });
            Assert.True(invoked);
            Assert.False(gate.IsHeldByCurrentThread);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingConstrainedExternalLockScopeIsRejectedBeforeInvocation()
        {
            bool invoked = false;
            this.TestWithError(() =>
            {
                var gate = new Lock();
                LockScopeProvider.ValueProvider provider = default;
                UseExternalLockScope(ref provider, gate, () => invoked = true);
            },
            configuration: this.GetConfiguration().WithSystematicFuzzingFallbackEnabled(false),
            errorChecker: (e) =>
            {
                Assert.Contains("IProvider.EnterScope", e);
                Assert.Contains("uncontrolled lock scope", e);
            });
            Assert.False(invoked);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingConstrainedExternalLockScopeFallsBackToFuzzing()
        {
            int completed = 0;
            this.Test(() =>
            {
                var gate = new Lock();
                LockScopeProvider.ValueProvider provider = default;
                UseExternalLockScope(ref provider, gate, () =>
                {
                    Assert.Equal(SchedulingPolicy.Fuzzing, CoyoteRuntime.Current.SchedulingPolicy);
                    Assert.True(gate.IsHeldByCurrentThread);
                });
                Assert.False(gate.IsHeldByCurrentThread);
                completed++;
            },
            configuration: this.GetConfiguration().WithSystematicFuzzingFallbackEnabled()
                .WithTestingIterations(3));
            Assert.True(completed > 0);
        }

        private static void UseExternalLockScope<T>(ref T provider, Lock gate, Action onAcquired)
            where T : struct, LockScopeProvider.IProvider
        {
            using (provider.EnterScope(gate, () => { }))
            {
                onAcquired();
            }
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestRewritingLockDelegatesShareOwnershipWithDirectCalls(bool isControlled)
        {
            Action test = () =>
            {
                var gate = new Lock();
                Action enter = gate.Enter;
                Action exit = gate.Exit;
                Func<bool> tryEnter = gate.TryEnter;
                Func<int, bool> tryEnterMilliseconds = gate.TryEnter;
                Func<TimeSpan, bool> tryEnterTimeSpan = gate.TryEnter;
                EnterScopeDelegate enterScope = gate.EnterScope;

                gate.Enter();
                exit();
                Assert.False(gate.IsHeldByCurrentThread);
                enter();
                Assert.True(gate.IsHeldByCurrentThread);
                gate.Exit();
                Assert.True(tryEnter());
                gate.Exit();
                Assert.True(tryEnterMilliseconds(0));
                gate.Exit();
                Assert.True(tryEnterTimeSpan(TimeSpan.Zero));
                gate.Exit();
                using (enterScope())
                {
                    Assert.True(gate.IsHeldByCurrentThread);
                }

                Assert.False(gate.IsHeldByCurrentThread);
                if (isControlled)
                {
                    gate.Enter();
                    try
                    {
                        Task.Run(() =>
                        {
                            Assert.False(tryEnter());
                            Assert.False(tryEnterMilliseconds(0));
                            Assert.False(tryEnterTimeSpan(TimeSpan.Zero));
                        }).GetAwaiter().GetResult();
                    }
                    finally
                    {
                        gate.Exit();
                    }
                }

                Assert.Throws<SynchronizationLockException>(exit);
                Assert.Throws<ArgumentOutOfRangeException>(() => tryEnterMilliseconds(-2));
                Assert.Throws<ArgumentOutOfRangeException>(() => tryEnterTimeSpan(TimeSpan.FromMilliseconds(-2)));
                Assert.Equal<Action>(gate.Enter, enter);
                Assert.Equal<Action>(gate.Exit, exit);
                var otherGate = new Lock();
                Assert.NotEqual<Action>(otherGate.Enter, enter);
            };

            if (isControlled)
            {
                this.Test(test);
            }
            else
            {
                test();
            }
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestRewritingLockDelegatesContendWithDirectCalls(bool useTryEnter)
        {
            var winners = new HashSet<int>();
            this.Test(() =>
            {
                var gate = new Lock();
                Action enter = gate.Enter;
                Action exit = gate.Exit;
                Func<int, bool> tryEnter = gate.TryEnter;
                int winner = 0;
                int entered = 0;
                Task first = Task.Run(() =>
                {
                    if (useTryEnter)
                    {
                        Assert.True(tryEnter(Timeout.Infinite));
                    }
                    else
                    {
                        enter();
                    }

                    Assert.True(gate.IsHeldByCurrentThread);
                    Assert.Equal(0, entered++);
                    if (winner is 0)
                    {
                        winner = 1;
                    }

                    SchedulingPoint.Interleave();
                    Assert.Equal(1, entered--);
                    gate.Exit();
                });
                Task second = Task.Run(() =>
                {
                    gate.Enter();
                    Assert.Equal(0, entered++);
                    if (winner is 0)
                    {
                        winner = 2;
                    }

                    SchedulingPoint.Interleave();
                    Assert.Equal(1, entered--);
                    exit();
                });
                Task.WaitAll(first, second);
                winners.Add(winner);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));

            Assert.Contains(1, winners);
            Assert.Contains(2, winners);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingNullLockDelegateCreationThrowsArgumentException()
        {
            this.Test(() =>
            {
                Lock gate = null;
                Assert.Throws<ArgumentException>(() =>
                {
                    Action enter = gate.Enter;
                    GC.KeepAlive(enter);
                });
                Assert.Throws<ArgumentException>(() =>
                {
                    Action exit = gate.Exit;
                    GC.KeepAlive(exit);
                });
                Assert.Throws<ArgumentException>(() =>
                {
                    Func<bool> tryEnter = gate.TryEnter;
                    GC.KeepAlive(tryEnter);
                });
                Assert.Throws<ArgumentException>(() =>
                {
                    EnterScopeDelegate enterScope = gate.EnterScope;
                    GC.KeepAlive(enterScope);
                });
            });
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
        public void TestRewritingNullLockReceiversThrowNullReferenceException()
        {
            this.Test(() =>
            {
                Lock gate = null;
                Assert.Throws<NullReferenceException>(() => gate.Enter());
                Assert.Throws<NullReferenceException>(() => gate.TryEnter());
                Assert.Throws<NullReferenceException>(() => gate.TryEnter(0));
                Assert.Throws<NullReferenceException>(() => gate.TryEnter(-2));
                Assert.Throws<NullReferenceException>(() => gate.TryEnter(TimeSpan.Zero));
                Assert.Throws<NullReferenceException>(() => gate.TryEnter(TimeSpan.FromTicks(-20_000)));
                Assert.Throws<NullReferenceException>(() => gate.TryEnter(TimeSpan.MaxValue));
                Assert.Throws<NullReferenceException>(() => gate.Exit());
                Assert.Throws<NullReferenceException>(() => _ = gate.IsHeldByCurrentThread);
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

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestRewritingLockTryEnterRetries(bool checkLockAccessRaces)
        {
            this.Test(() =>
            {
                var gate = new Lock();
                int entered = 0;
                Task first = Task.Run(() => TryEnter(gate, ref entered));
                Task second = Task.Run(() => TryEnter(gate, ref entered));
                Task.WaitAll(first, second);
            },
            configuration: this.GetConfiguration().WithRandomStrategy().WithRandomGeneratorSeed(0)
                .WithLockAccessRaceCheckingEnabled(checkLockAccessRaces).WithTestingIterations(100));
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
                winners.Add(winner);
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
            int attempts = 0;
            while (!gate.TryEnter())
            {
                Specification.Assert(++attempts < 100, "Lock retries did not schedule the owner.");
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
