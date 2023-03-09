// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class AutoResetEventTests : BaseBugFindingTest
    {
        public AutoResetEventTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestWaitSetReset()
        {
            this.Test(() =>
            {
                using AutoResetEvent evt = new AutoResetEvent(false);
                bool result = evt.WaitOne(0);
                Specification.Assert(!result, "1st assertion failed.");
                result = evt.Set();
                Specification.Assert(result, "2nd assertion failed.");
                result = evt.WaitOne(0);
                Specification.Assert(result, "3rd assertion failed.");
                result = evt.WaitOne(0);
                Specification.Assert(!result, "4rd assertion failed.");
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
                using AutoResetEvent evt = new AutoResetEvent(true);
                bool result = evt.WaitOne();
                Specification.Assert(result, "1st assertion failed.");
                result = evt.WaitOne(0);
                Specification.Assert(!result, "2nd assertion failed.");
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWaitDeadlock()
        {
            this.TestWithError(() =>
            {
                using AutoResetEvent evt = new AutoResetEvent(false);
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
                AutoResetEvent[] handles = new AutoResetEvent[4];
                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i] = new AutoResetEvent(true);
                }

                try
                {
                    Specification.Assert(WaitHandle.WaitAny(handles, 0) is 0, "1st assertion failed.");
                    for (int i = 0; i < handles.Length; ++i)
                    {
                        Specification.Assert(handles[i].WaitOne(0) == i > 0, "2nd assertion failed.");
                        handles[i].Set();
                    }

                    Specification.Assert(WaitHandle.WaitAny(handles) is 0, "3rd assertion failed.");
                    for (int i = 0; i < handles.Length; ++i)
                    {
                        Specification.Assert(handles[i].WaitOne(0) == i > 0, "4th assertion failed.");
                        handles[i].Set();
                    }

                    Specification.Assert(WaitHandle.WaitAny(handles) is 0, "5th assertion failed.");
                    for (int i = 0; i < handles.Length; ++i)
                    {
                        Specification.Assert(handles[i].WaitOne(0) == i > 0, "6th assertion failed.");
                        handles[i].Set();
                    }

                    Specification.Assert(WaitHandle.WaitAll(handles, 0), "7th assertion failed.");
                    for (int i = 0; i < handles.Length; ++i)
                    {
                        Specification.Assert(!handles[i].WaitOne(0), "8th assertion failed.");
                        handles[i].Set();
                    }

                    Specification.Assert(WaitHandle.WaitAll(handles), "9th assertion failed.");
                    for (int i = 0; i < handles.Length; ++i)
                    {
                        Specification.Assert(!handles[i].WaitOne(0), "10th assertion failed.");
                        handles[i].Set();
                    }

                    Specification.Assert(WaitHandle.WaitAll(handles), "11th assertion failed.");
                    for (int i = 0; i < handles.Length; ++i)
                    {
                        Specification.Assert(!handles[i].WaitOne(0), "12th assertion failed.");
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
                AutoResetEvent[] handles = new AutoResetEvent[4];
                handles[0] = new AutoResetEvent(false);
                handles[1] = new AutoResetEvent(true);
                handles[2] = new AutoResetEvent(true);
                handles[3] = new AutoResetEvent(false);

                try
                {
                    Specification.Assert(WaitHandle.WaitAny(handles, 0) is 1, "1st assertion failed.");
                    for (int i = 0; i < handles.Length; ++i)
                    {
                        bool expected = i == 2;
                        Specification.Assert(handles[i].WaitOne(0) == expected, "2nd assertion failed.");
                    }

                    handles[1].Set();
                    handles[2].Set();

                    Specification.Assert(WaitHandle.WaitAny(handles) is 1, "3rd assertion failed.");
                    for (int i = 0; i < handles.Length; ++i)
                    {
                        bool expected = i == 2;
                        Specification.Assert(handles[i].WaitOne(0) == expected, "4th assertion failed.");
                    }

                    handles[1].Set();
                    handles[2].Set();

                    Specification.Assert(!WaitHandle.WaitAll(handles, 0), "5th assertion failed.");
                    // Specification.Assert(!WaitHandle.WaitAll(handles), "11th assertion failed.");
                    for (int i = 0; i < handles.Length; ++i)
                    {
                        bool expected = i == 1 || i == 2;
                        Specification.Assert(handles[i].WaitOne(0) == expected, "6th assertion failed.");
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
                AutoResetEvent[] handles = new AutoResetEvent[4];
                handles[0] = new AutoResetEvent(false);
                handles[1] = new AutoResetEvent(false);
                handles[2] = new AutoResetEvent(false);
                handles[3] = new AutoResetEvent(false);

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
                using AutoResetEvent evt = new AutoResetEvent(false);
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
                using AutoResetEvent evt = new AutoResetEvent(false);
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
                AutoResetEvent[] handles = new AutoResetEvent[10];
                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i] = new AutoResetEvent(false);
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
                AutoResetEvent[] handles = new AutoResetEvent[10];
                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i] = new AutoResetEvent(false);
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
                using AutoResetEvent evt1 = new AutoResetEvent(true);
                using AutoResetEvent evt2 = new AutoResetEvent(false);

                Thread t1 = new Thread(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        evt1.WaitOne();
                        evt2.Set();
                    }
                });

                Thread t2 = new Thread(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        evt2.WaitOne();
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
