// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

using Monitor = System.Threading.Monitor;
using SynchronizedBlock = Microsoft.Coyote.Rewriting.Types.Threading.Monitor.SynchronizedBlock;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class MonitorTests : BaseBugFindingTest
    {
        public MonitorTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestSimpleMonitor()
        {
            this.Test(async () =>
            {
                SignalData signal = new SignalData();
                var t1 = Task.Run(signal.Wait);
                var t2 = Task.Run(signal.Signal);
                await Task.WhenAll(t1, t2);
            },
            this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorWithReentrancy1()
        {
            this.Test(() =>
            {
                SignalData signal = new SignalData();
                signal.ReentrantLock();
            },
            this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorWithReentrancy2()
        {
            this.Test(async () =>
            {
                SignalData signal = new SignalData();
                Task t1 = Task.Run(signal.ReentrantLock);
                Task t2 = Task.Run(signal.DoLock);
                await Task.WhenAll(t1, t2);
            },
            this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorWithReentrancy3()
        {
            this.Test(async () =>
            {
                SignalData signal = new SignalData();
                Task t1 = Task.Run(signal.ReentrantWait);
                Task t2 = Task.Run(signal.Signal);
                await Task.WhenAll(t1, t2);
            },
            this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorWithInvalidSyncObject()
        {
            this.TestWithException<ArgumentNullException>(() =>
            {
                using var monitor = SynchronizedBlock.Lock(null);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorWithInvalidWaitState()
        {
            this.TestWithException<SynchronizationLockException>(() =>
            {
                SynchronizedBlock monitor;
                using (monitor = SynchronizedBlock.Lock(new object()))
                {
                }

                monitor.Wait();
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorWithInvalidPulseState()
        {
            this.TestWithException<SynchronizationLockException>(() =>
            {
                SynchronizedBlock monitor;
                using (monitor = SynchronizedBlock.Lock(new object()))
                {
                }

                monitor.Pulse();
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorWithInvalidPulseAllState()
        {
            this.TestWithException<SynchronizationLockException>(() =>
            {
                SynchronizedBlock monitor;
                using (monitor = SynchronizedBlock.Lock(new object()))
                {
                }

                monitor.PulseAll();
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorWithInvalidUsage()
        {
            this.TestWithError(async () =>
            {
                try
                {
                    var monitor = SynchronizedBlock.Lock(new object());
                    // We yield to make sure the execution is asynchronous.
                    await Task.Yield();
                    monitor.Pulse();

                    // We do not dispose inside a using statement, because the `SynchronizationLockException`
                    // will trigger the disposal, which will fail because an await statement is not allowed
                    // inside a synchronized block. The C# compiler normally prevents it when using the lock
                    // statement, but we cannot prevent it when directly using the mock.
                    monitor.Dispose();
                }
                catch (SynchronizationLockException)
                {
                    Specification.Assert(false, "Expected exception thrown.");
                }
            },
            expectedError: "Expected exception thrown.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorWithConcurrentTryLock()
        {
            this.Test(async () =>
            {
                TryLockData data = new TryLockData();
                Task t1 = Task.Run(data.AcquireAndRelease);
                Task t2 = Task.Run(data.AcquireAndRelease);
                await Task.WhenAll(t1, t2);
            },
            this.GetConfiguration().WithRandomStrategy().WithRandomGeneratorSeed(0)
                .WithLockAccessRaceCheckingEnabled().WithTestingIterations(100));
        }

        [Theory(Timeout = 5000)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void TestMonitorTryEnterWithoutWaitingWithContention(int overload)
        {
            this.Test(() =>
            {
                object syncObject = new object();
                Monitor.Enter(syncObject);
                try
                {
                    Task worker = Task.Run(() =>
                    {
                        bool lockTaken = TryEnterWithoutWaiting(syncObject, overload);
                        try
                        {
                            Specification.Assert(!lockTaken, "The worker acquired the parent's monitor.");
                        }
                        finally
                        {
                            if (lockTaken)
                            {
                                Monitor.Exit(syncObject);
                            }
                        }
                    });

                    worker.Wait();
                    Specification.Assert(Monitor.IsEntered(syncObject), "The parent lost its monitor.");
                }
                finally
                {
                    Monitor.Exit(syncObject);
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(20)
                .WithLockAccessRaceCheckingEnabled());
        }

        [Theory(Timeout = 5000)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void TestMonitorTryEnterWithoutWaitingWithReentrancy(int overload)
        {
            this.Test(() =>
            {
                object syncObject = new object();
                bool lockTaken = TryEnterWithoutWaiting(syncObject, overload);
                Specification.Assert(lockTaken, "The free monitor was not acquired.");
                try
                {
                    bool reentered = TryEnterWithoutWaiting(syncObject, overload);
                    Specification.Assert(reentered, "The owner could not reenter its monitor.");
                    try
                    {
                        Specification.Assert(Monitor.IsEntered(syncObject), "The monitor has no owner.");
                    }
                    finally
                    {
                        Monitor.Exit(syncObject);
                    }

                    Specification.Assert(Monitor.IsEntered(syncObject), "The outer lock was released early.");
                }
                finally
                {
                    Monitor.Exit(syncObject);
                }

                Specification.Assert(!Monitor.IsEntered(syncObject), "The monitor was not released.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(20)
                .WithLockAccessRaceCheckingEnabled());
        }

        private static bool TryEnterWithoutWaiting(object syncObject, int overload)
        {
            bool lockTaken = false;
            switch (overload)
            {
                case 0:
                    return Monitor.TryEnter(syncObject, 0);
                case 1:
                    Monitor.TryEnter(syncObject, 0, ref lockTaken);
                    return lockTaken;
                case 2:
                    return Monitor.TryEnter(syncObject, TimeSpan.Zero);
                case 3:
                    Monitor.TryEnter(syncObject, TimeSpan.Zero, ref lockTaken);
                    return lockTaken;
                case 4:
                    return Monitor.TryEnter(syncObject);
                case 5:
                    Monitor.TryEnter(syncObject, ref lockTaken);
                    return lockTaken;
                default:
                    throw new ArgumentOutOfRangeException(nameof(overload));
            }
        }

        [Theory(Timeout = 5000)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void TestMonitorTryEnterRejectsInvalidTimeoutBeforeAcquisition(int overload)
        {
            this.Test(() =>
            {
                object gate = new object();
                bool lockTaken = false;
                Assert.Throws<ArgumentOutOfRangeException>(() => TryEnterWithTimeout(gate, overload, -2, ref lockTaken));
                Assert.False(lockTaken);
                Assert.False(Monitor.IsEntered(gate));

                Monitor.Enter(gate);
                try
                {
                    Task worker = Task.Run(() =>
                    {
                        bool taken = false;
                        Assert.Throws<ArgumentOutOfRangeException>(() => TryEnterWithTimeout(gate, overload, -2, ref taken));
                        Assert.False(taken);
                    });
                    worker.Wait();
                }
                finally
                {
                    Monitor.Exit(gate);
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(20));
        }

        [Theory(Timeout = 5000)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void TestMonitorTryEnterPreservesArgumentValidationOrder(int overload)
        {
            this.Test(() =>
            {
                bool lockTaken = false;
                Assert.Throws<ArgumentNullException>(() => TryEnterWithTimeout(null, overload, 0, ref lockTaken));
                if (overload < 2)
                {
                    Assert.Throws<ArgumentNullException>(() => TryEnterWithTimeout(null, overload, -2, ref lockTaken));
                }
                else
                {
                    var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                        TryEnterWithTimeout(null, overload, -2, ref lockTaken));
                    Assert.Equal("timeout", exception.ParamName);
                }

                if (overload is 1 || overload is 3)
                {
                    lockTaken = true;
                    if (overload is 1)
                    {
                        var exception = Assert.Throws<ArgumentException>(() =>
                            TryEnterWithTimeout(null, overload, -2, ref lockTaken));
                        Assert.Equal("lockTaken", exception.ParamName);
                    }
                    else
                    {
                        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                            TryEnterWithTimeout(null, overload, -2, ref lockTaken));
                        Assert.Equal("timeout", exception.ParamName);
                    }

                    Assert.True(lockTaken);
                }
            });
        }

        [Theory(Timeout = 5000)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void TestMonitorTryEnterAcceptsValidTimeouts(int overload)
        {
            this.Test(() =>
            {
                object gate = new object();
                foreach (int timeout in new[] { -1, 0, 1, int.MaxValue })
                {
                    bool lockTaken = false;
                    Assert.True(TryEnterWithTimeout(gate, overload, timeout, ref lockTaken));
                    Monitor.Exit(gate);
                }
            });
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorTryEnterRejectsInitiallyTakenFlag()
        {
            this.Test(() =>
            {
                object gate = new object();
                bool lockTaken = true;
                var exception = Assert.Throws<ArgumentException>(() => Monitor.TryEnter(gate, ref lockTaken));
                Assert.Equal("lockTaken", exception.ParamName);
                Assert.True(lockTaken);
                Assert.False(Monitor.IsEntered(gate));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorTryEnterValidatesTimeSpanRange()
        {
            this.Test(() =>
            {
                object gate = new object();
                bool lockTaken = false;
                TimeSpan tooLarge = TimeSpan.FromMilliseconds((long)int.MaxValue + 1);
                Assert.Throws<ArgumentOutOfRangeException>(() => Monitor.TryEnter(gate, tooLarge));
                Assert.Throws<ArgumentOutOfRangeException>(() => Monitor.TryEnter(gate, tooLarge, ref lockTaken));
                Assert.False(lockTaken);
                Assert.False(Monitor.IsEntered(gate));

                foreach (double milliseconds in new[] { -1.5, 0.5, int.MaxValue + 0.5 })
                {
                    Assert.True(Monitor.TryEnter(gate, TimeSpan.FromMilliseconds(milliseconds)));
                    Monitor.Exit(gate);
                }
            });
        }

        [Theory(Timeout = 5000)]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(4, true)]
        [InlineData(5, false)]
        [InlineData(5, true)]
        public void TestMonitorTryEnterRetriesAllowOwnerToRelease(int overload, bool checkLockAccessRaces)
        {
            this.Test(() =>
            {
                object gate = new object();
                Monitor.Enter(gate);
                Task contender;
                try
                {
                    contender = Task.Run(() =>
                    {
                        int attempts = 0;
                        while (!TryEnterWithoutWaiting(gate, overload))
                        {
                            Specification.Assert(++attempts < 100, "Lock retries did not schedule the owner.");
                        }

                        Monitor.Exit(gate);
                    });
                    SchedulingPoint.Interleave();
                }
                finally
                {
                    Monitor.Exit(gate);
                }

                contender.Wait();
                Assert.False(Monitor.IsEntered(gate));
            },
            configuration: this.GetConfiguration().WithRandomStrategy().WithRandomGeneratorSeed(0).WithTestingIterations(50)
                .WithLockAccessRaceCheckingEnabled(checkLockAccessRaces));
        }

        private static bool TryEnterWithTimeout(object syncObject, int overload, int millisecondsTimeout,
            ref bool lockTaken)
        {
            switch (overload)
            {
                case 0:
                    return Monitor.TryEnter(syncObject, millisecondsTimeout);
                case 1:
                    Monitor.TryEnter(syncObject, millisecondsTimeout, ref lockTaken);
                    return lockTaken;
                case 2:
                    return Monitor.TryEnter(syncObject, TimeSpan.FromMilliseconds(millisecondsTimeout));
                case 3:
                    Monitor.TryEnter(syncObject, TimeSpan.FromMilliseconds(millisecondsTimeout), ref lockTaken);
                    return lockTaken;
                default:
                    throw new ArgumentOutOfRangeException(nameof(overload));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestComplexMonitor()
        {
            this.Test(async () =>
            {
                object syncObject = new object();
                bool waiting = false;
                List<string> log = new List<string>();
                Task t1 = Task.Run(() =>
                {
                    Monitor.Enter(syncObject);
                    log.Add("waiting");
                    waiting = true;
                    Monitor.Wait(syncObject);
                    log.Add("received pulse");
                    Monitor.Exit(syncObject);
                });

                Task t2 = Task.Run(async () =>
                {
                    while (!waiting)
                    {
                        await Task.Delay(1);
                    }

                    Monitor.Enter(syncObject);
                    Monitor.Pulse(syncObject);
                    log.Add("pulsed");
                    Monitor.Exit(syncObject);
                });

                await Task.WhenAll(t1, t2);

                string expected = "waiting, pulsed, received pulse";
                string actual = string.Join(", ", log);
                Specification.Assert(expected == actual, "ControlledMonitor out of order, '{0}' instead of '{1}'", actual, expected);
            },
            this.GetConfiguration());
        }

        private class TryLockData
        {
            private readonly object SyncObject;
            private int EnteredCount;

            internal TryLockData()
            {
                this.SyncObject = new object();
                this.EnteredCount = 0;
            }

            internal void AcquireAndRelease()
            {
                int attempts = 0;
                while (!SynchronizedBlock.TryLock(this.SyncObject))
                {
                    Specification.Assert(++attempts < 100, "Lock retries did not schedule the owner.");
                }

                this.EnteredCount++;
                Specification.Assert(this.EnteredCount is 1,
                    "More than one operation acquired the lock, expected 1 but found {0}.", this.EnteredCount);
                SchedulingPoint.Interleave();
                this.EnteredCount--;
                Monitor.Exit(this.SyncObject);
            }
        }

        private class SignalData
        {
            private readonly object SyncObject;
            internal bool Signalled;

            internal SignalData()
            {
                this.SyncObject = new object();
                this.Signalled = false;
            }

            internal void Signal()
            {
                using var monitor = SynchronizedBlock.Lock(this.SyncObject);
                this.Signalled = true;
                monitor.Pulse();
            }

            internal void Wait()
            {
                using var monitor = SynchronizedBlock.Lock(this.SyncObject);
                while (!this.Signalled)
                {
                    bool result = monitor.Wait();
                    Assert.True(result, "Wait returned false.");
                }
            }

            internal void ReentrantLock()
            {
                Debug.WriteLine("Entering lock on task {0}.", GetCurrentTaskId());
                using var monitor = SynchronizedBlock.Lock(this.SyncObject);
                Debug.WriteLine("Entered lock on task {0}.", GetCurrentTaskId());
                this.DoLock();
            }

            internal void DoLock()
            {
                using var monitor = SynchronizedBlock.Lock(this.SyncObject);
                Debug.WriteLine("Re-entered lock from the same task {0}.", GetCurrentTaskId());
            }

            internal void ReentrantWait()
            {
                Debug.WriteLine("Entering lock on task {0}.", GetCurrentTaskId());
                using var monitor = SynchronizedBlock.Lock(this.SyncObject);
                Debug.WriteLine("Entered lock on task {0}.", GetCurrentTaskId());
                this.DoWait();
            }

            internal void DoWait()
            {
                using var monitor = SynchronizedBlock.Lock(this.SyncObject);
                Debug.WriteLine("Re-entered lock from the same task {0}.", GetCurrentTaskId());
                Debug.WriteLine("Task {0} is now waiting...", GetCurrentTaskId());
                this.Wait();
                Debug.WriteLine("Task {0} received the signal.", GetCurrentTaskId());
            }

            internal static int GetCurrentTaskId() => Task.CurrentId ?? 0;
        }
    }
}
