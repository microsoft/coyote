// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class SynchronizedBlockTests : BaseTest
    {
        public SynchronizedBlockTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizedBlockWithInvalidSyncObject()
        {
            this.TestWithException<ArgumentNullException>(() =>
            {
                using (var monitor = SynchronizedBlock.Lock(null))
                {
                }
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizedBlockWithInvalidWaitState()
        {
            this.TestWithError(() =>
            {
                object syncObject = new object();
                SynchronizedBlock monitor;
                using (monitor = SynchronizedBlock.Lock(syncObject))
                {
                }
                monitor.Wait();
            },
            expectedErrors: new string[]
            {
                "Cannot invoke Wait without first taking the lock.",
                "Object synchronization method was called from an unsynchronized block of code.",
                "Cannot use a disposed SyncObjectState"
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizedBlockWithInvalidPulseState()
        {
            this.TestWithError(() =>
            {
                object syncObject = new object();
                SynchronizedBlock monitor;
                using (monitor = SynchronizedBlock.Lock(syncObject))
                {
                }
                monitor.Pulse();
            },
            expectedErrors: new string[]
            {
                "Cannot invoke Pulse without first taking the lock.",
                "Object synchronization method was called from an unsynchronized block of code.",
                "Cannot use a disposed SyncObjectState"
            },
            replay: true);
        }

        private class SignalData
        {
            internal object SyncObject = new object();
            public bool Waiting;

            internal async Task Signal()
            {
                while (!this.Waiting)
                {
                    await Task.Delay(1);
                }

                using (var monitor = SynchronizedBlock.Lock(this.SyncObject))
                {
                    monitor.Pulse();
                }
            }

            internal bool Wait()
            {
                using (var monitor = SynchronizedBlock.Lock(this.SyncObject))
                {
                    this.Waiting = true;
                    return monitor.Wait();
                }
            }

            internal void ReentrantLock()
            {
                Debug.WriteLine("Entering lock on task {0}.", GetCurrentTaskId());
                using (var monitor = SynchronizedBlock.Lock(this.SyncObject))
                {
                    Debug.WriteLine("Entered lock on task {0}.", GetCurrentTaskId());
                    this.DoLock();
                }
            }

            internal void DoLock()
            {
                using (var monitor = SynchronizedBlock.Lock(this.SyncObject))
                {
                    Debug.WriteLine("Re-entered lock from the same task {0}.", GetCurrentTaskId());
                }
            }

            internal void ReentrantWait()
            {
                Debug.WriteLine("Entering lock on task {0}.", GetCurrentTaskId());
                using (var monitor = SynchronizedBlock.Lock(this.SyncObject))
                {
                    Debug.WriteLine("Entered lock on task {0}.", GetCurrentTaskId());
                    this.DoWait();
                }
            }

            internal void DoWait()
            {
                using (var monitor = SynchronizedBlock.Lock(this.SyncObject))
                {
                    Debug.WriteLine("Re-entered lock from the same task {0}.", GetCurrentTaskId());
                    Debug.WriteLine("Task {0} is now waiting...", GetCurrentTaskId());
                    this.Wait();
                    Debug.WriteLine("Task {0} received the signal.", GetCurrentTaskId());
                }
            }

            internal static int GetCurrentTaskId()
            {
                var v = Task.CurrentId;
                if (v.HasValue)
                {
                    return v.Value;
                }

                return 0;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSimpleSynchronizedBlock()
        {
            this.Test(() =>
            {
                bool waited = false;
                SignalData signal = new SignalData();
                var t1 = Task.Run(() => waited = signal.Wait());
                var t2 = Task.Run(() => signal.Signal());
                Task.WaitAll(t1, t2);
                Assert.True(waited, "Wait returned false?");
            },
            Configuration.Create().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizedBlockWithReentrancy1()
        {
            this.Test(() =>
            {
                SignalData signal = new SignalData();
                signal.ReentrantLock();
            },
            Configuration.Create().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizedBlockWithReentrancy2()
        {
            this.Test(() =>
            {
                SignalData signal = new SignalData();
                Task t1 = Task.Run(signal.ReentrantLock);
                Task t2 = Task.Run(signal.DoLock);
                Task.WaitAll(t1, t2);
            },
            Configuration.Create().WithTestingIterations(1));
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizedBlockWithReentrancy3()
        {
            this.Test(() =>
            {
                SignalData signal = new SignalData();
                Task t1 = Task.Run(signal.ReentrantWait);
                Task t2 = Task.Run(signal.Signal);
                Task.WaitAll(t1, t2);
            },
            Configuration.Create().WithTestingIterations(1));
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizedBlockWithInvalidUsage()
        {
            if (!this.SystematicTest)
            {
                // bugbug: don't know why but the build machines are hanging on this test, but we cannot
                // reproduce this hang locally.
                return;
            }

            this.TestWithError(() =>
            {
                object syncObject = new object();
                using (var monitor = SynchronizedBlock.Lock(syncObject))
                {
                    var t1 = Task.Run(() => monitor.Wait());
                    var t2 = Task.Run(() => monitor.Pulse());
                    Task.WaitAll(t1, t2);
                }
            },
            errorChecker: (e) =>
            {
                Assert.True(e.Contains("Object synchronization method was called from an unsynchronized block of code.") ||
                    e.Contains("Object synchronization method was called from a task that did not create this SynchronizedBlock."),
                "Expected 'Object synchronization method was called from an unsynchronized block of code', but found error: " + e);
            },
            replay: true);
        }
    }
}
