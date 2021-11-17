// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

using Monitor = System.Threading.Monitor;
using SynchronizedBlock = Microsoft.Coyote.Rewriting.Types.SynchronizedBlock;

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
