// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class ManualResetEventTests : BaseBugFindingTest
    {
        public ManualResetEventTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestWaitSetReset()
        {
            this.Test(() =>
            {
                using ManualResetEvent evt = new ManualResetEvent(false);
                bool result = evt.WaitOne(0);
                Specification.Assert(!result, "1st assertion failed.");
                result = evt.Set();
                Specification.Assert(result, "2nd assertion failed.");
                result = evt.WaitOne(0);
                Specification.Assert(result, "3rd assertion failed.");
                result = evt.WaitOne(0);
                Specification.Assert(result, "4rd assertion failed.");
                result = evt.Reset();
                Specification.Assert(result, "5th assertion failed.");
                result = evt.WaitOne(0);
                Specification.Assert(!result, "6th assertion failed.");
                result = evt.WaitOne(0);
                Specification.Assert(!result, "7th assertion failed.");
                result = evt.Set();
                Specification.Assert(result, "8th assertion failed.");
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAlreadySignaled()
        {
            this.Test(() =>
            {
                using ManualResetEvent evt = new ManualResetEvent(true);
                bool result = evt.WaitOne();
                Specification.Assert(result, "1st assertion failed.");
                result = evt.WaitOne();
                Specification.Assert(result, "2nd assertion failed.");
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWaitDeadlock()
        {
            this.TestWithError(() =>
            {
                using ManualResetEvent evt = new ManualResetEvent(false);
                evt.WaitOne();
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitWithAllIndexesSet()
        {
            this.Test(() =>
            {
                ManualResetEvent[] handles = new ManualResetEvent[4];
                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i] = new ManualResetEvent(true);
                }

                try
                {
                    Specification.Assert(WaitHandle.WaitAny(handles, 0) is 0, "1st assertion failed.");
                    Specification.Assert(WaitHandle.WaitAny(handles) is 0, "2nd assertion failed.");
                    Specification.Assert(WaitHandle.WaitAny(handles) is 0, "3rd assertion failed.");
                    Specification.Assert(WaitHandle.WaitAll(handles, 0), "4th assertion failed.");
                    Specification.Assert(WaitHandle.WaitAll(handles), "5th assertion failed.");
                    Specification.Assert(WaitHandle.WaitAll(handles), "6th assertion failed.");
                    for (int i = 0; i < handles.Length; ++i)
                    {
                        Specification.Assert(handles[i].WaitOne(0), "7th assertion failed.");
                    }
                }
                finally
                {
                    foreach (var handle in handles)
                    {
                        handle.Dispose();
                    }
                }
            },
            configuration: this.GetConfiguration());
        }

        [Fact(Timeout = 5000)]
        public void TestWaitWithInnerIndexesSet()
        {
            this.Test(() =>
            {
                ManualResetEvent[] handles = new ManualResetEvent[4];
                handles[0] = new ManualResetEvent(false);
                handles[1] = new ManualResetEvent(true);
                handles[2] = new ManualResetEvent(true);
                handles[3] = new ManualResetEvent(false);

                try
                {
                    Specification.Assert(WaitHandle.WaitAny(handles, 0) is 1, "1st assertion failed.");
                    Specification.Assert(WaitHandle.WaitAny(handles) is 1, "2nd assertion failed.");
                    Specification.Assert(!WaitHandle.WaitAll(handles, 0), "3rd assertion failed.");
                    // Specification.Assert(!WaitHandle.WaitAll(handles), "4th assertion failed.");
                    for (int i = 0; i < handles.Length; ++i)
                    {
                        bool expected = i == 1 || i == 2;
                        Specification.Assert(handles[i].WaitOne(0) == expected, "5th assertion failed.");
                    }
                }
                finally
                {
                    foreach (var handle in handles)
                    {
                        handle.Dispose();
                    }
                }
            },
            configuration: this.GetConfiguration());
        }

        [Fact(Timeout = 5000)]
        public void TestWaitWithAllIndexesReset()
        {
            this.Test(() =>
            {
                ManualResetEvent[] handles = new ManualResetEvent[4];
                handles[0] = new ManualResetEvent(false);
                handles[1] = new ManualResetEvent(false);
                handles[2] = new ManualResetEvent(false);
                handles[3] = new ManualResetEvent(false);

                try
                {
                    Specification.Assert(WaitHandle.WaitAny(handles, 0) is 0, "1st assertion failed.");
                    // Specification.Assert(WaitHandle.WaitAny(handles) is 0, "2nd assertion failed.");
                    Specification.Assert(!WaitHandle.WaitAll(handles, 0), "3rd assertion failed.");
                    // Specification.Assert(!WaitHandle.WaitAll(handles), "4th assertion failed.");
                    for (int i = 0; i < handles.Length; ++i)
                    {
                        bool expected = i == 1 || i == 2;
                        Specification.Assert(!handles[i].WaitOne(0), "5th assertion failed.");
                    }
                }
                finally
                {
                    foreach (var handle in handles)
                    {
                        handle.Dispose();
                    }
                }
            },
            configuration: this.GetConfiguration());
        }

        [Fact(Timeout = 5000)]
        public void TestMultiThreadedWait()
        {
            this.Test(() =>
            {
                using ManualResetEvent evt = new ManualResetEvent(false);
                Thread t1 = new Thread(() =>
                {
                    evt.Set();
                });

                bool result = false;
                Thread t2 = new Thread(() =>
                {
                    result = evt.WaitOne();
                });

                t1.Start();
                t2.Start();

                t1.Join();
                t2.Join();

                Specification.Assert(result, "Waiting the event failed.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestMultiThreadedWaitDeadlock()
        {
            this.TestWithError(() =>
            {
                using ManualResetEvent evt = new ManualResetEvent(false);
                Thread t1 = new Thread(() =>
                {
                    for (int i = 0; i < 2; i++)
                    {
                        evt.Set();
                    }
                });

                Thread t2 = new Thread(() =>
                {
                    for (int i = 0; i < 2; i++)
                    {
                        evt.WaitOne();
                        evt.Reset();
                    }
                });

                t1.Start();
                t2.Start();

                t1.Join();
                t2.Join();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAll()
        {
            this.Test(() =>
            {
                ManualResetEvent[] handles = new ManualResetEvent[10];
                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i] = new ManualResetEvent(false);
                }

                try
                {
                    Task t = Task.Run(() => WaitHandle.WaitAll(handles));
                    foreach (var handle in handles)
                    {
                        Specification.Assert(!t.IsCompleted, "Task is not blocked.");
                        handle.Set();
                    }

                    t.Wait();
                    Specification.Assert(t.IsCompleted, "Task is not completed.");
                }
                finally
                {
                    foreach (var handle in handles)
                    {
                        handle.Dispose();
                    }
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAny()
        {
            this.Test(() =>
            {
                ManualResetEvent[] handles = new ManualResetEvent[10];
                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i] = new ManualResetEvent(false);
                }

                try
                {
                    Task<int> t = Task.Run(() => WaitHandle.WaitAny(handles));
                    Specification.Assert(!t.IsCompleted, "Task is not blocked.");
                    handles[5].Set();
                    t.Wait();
                    Specification.Assert(t.IsCompleted, "Task is not completed.");
                    Specification.Assert(t.Result is 5, "Task result is not expected.");
                }
                finally
                {
                    foreach (var handle in handles)
                    {
                        handle.Dispose();
                    }
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestPingPong()
        {
            this.Test(() =>
            {
                using ManualResetEvent evt1 = new ManualResetEvent(true);
                using ManualResetEvent evt2 = new ManualResetEvent(false);

                Thread t1 = new Thread(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        evt1.WaitOne();
                        evt1.Reset();
                        evt2.Set();
                    }
                });

                Thread t2 = new Thread(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        evt2.WaitOne();
                        evt2.Reset();
                        evt1.Set();
                    }
                });

                t1.Start();
                t2.Start();

                t1.Join();
                t2.Join();
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }
    }
}
