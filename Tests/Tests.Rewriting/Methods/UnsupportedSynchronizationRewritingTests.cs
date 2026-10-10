// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
#if NET
using Microsoft.Coyote.Tests.Common.Threading;
#endif
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class UnsupportedSynchronizationRewritingTests : BaseRewritingTest
    {
        public UnsupportedSynchronizationRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestBarrierIsReportedAsUncontrolledSynchronization()
        {
            this.TestWithError(() =>
            {
                using var barrier = new Barrier(1);
                barrier.SignalAndWait();
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith(
                    $"Invoking '{typeof(Barrier).FullName}..ctor' is not intercepted",
                    e);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestCountdownEventIsReportedAsUncontrolledSynchronization()
        {
            this.TestWithError(() =>
            {
                using var countdown = new CountdownEvent(1);
                countdown.Signal();
                countdown.Wait();
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith(
                    $"Invoking '{typeof(CountdownEvent).FullName}..ctor' is not intercepted",
                    e);
            });
        }

#if NET
        private const string NamedEventError = "Creating the named system event '{0}' is not supported in systematic testing";

        [Fact(Timeout = 5000)]
        public void TestNativeNullAndEmptyEventNamesAreUnnamed()
        {
            // The rejection relies on this native contract: a null or empty name creates a new unnamed
            // event that is not shared, exactly as the unnamed constructor does.
            Assert.False(NamedEventProvider.IsShared(null));
            Assert.False(NamedEventProvider.IsShared(string.Empty));
            if (OperatingSystem.IsWindows())
            {
                Assert.True(NamedEventProvider.IsShared(NamedEventProvider.CreateUniqueName()));
            }
        }

        [Theory(Timeout = 5000)]
        [InlineData(0)]
        [InlineData(1)]
#if NET10_0_OR_GREATER
        [InlineData(2)]
        [InlineData(3)]
#endif
        public void TestNamedEventCreationIsRejected(int overload)
        {
            string name = NamedEventProvider.CreateUniqueName();
            bool isCreated = false;
            this.TestWithError(() =>
            {
                using EventWaitHandle handle = CreateEvent(overload, name);
                isCreated = true;
            },
            errorChecker: (e) => Assert.StartsWith(string.Format(NamedEventError, name), e));

            Assert.False(isCreated);
            if (OperatingSystem.IsWindows())
            {
                Assert.False(NamedEventProvider.Exists(name));
            }
        }

        [Theory(Timeout = 5000)]
        [InlineData(0, null)]
        [InlineData(0, "")]
        [InlineData(1, null)]
        [InlineData(1, "")]
#if NET10_0_OR_GREATER
        [InlineData(2, null)]
        [InlineData(2, "")]
        [InlineData(3, null)]
        [InlineData(3, "")]
#endif
        public void TestUnnamedEventCreationIsControlled(int overload, string name)
        {
            this.Test(async () =>
            {
                using EventWaitHandle handle = CreateEvent(overload, name);
                Task setter = Task.Run(() => handle.Set());
                handle.WaitOne();
                await setter;
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestUnnamedConstructorIsControlled()
        {
            this.Test(async () =>
            {
                using var handle = new EventWaitHandle(false, EventResetMode.AutoReset);
                Task setter = Task.Run(() => handle.Set());
                handle.WaitOne();
                await setter;
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestNamedEventOpeningIsReportedAsUncontrolledSynchronization()
        {
            if (!OperatingSystem.IsWindows())
            {
                // Opening a named event is only supported on Windows.
                return;
            }

            string name = NamedEventProvider.CreateUniqueName();
            using EventWaitHandle existing = NamedEventProvider.CreateNamed(name);
            bool isOpened = false;
            this.TestWithError(() =>
            {
                if (OperatingSystem.IsWindows())
                {
                    using EventWaitHandle handle = EventWaitHandle.OpenExisting(name);
                    isOpened = true;
                }
            },
            errorChecker: (e) => Assert.StartsWith(
                $"Invoking '{typeof(EventWaitHandle).FullName}.{nameof(EventWaitHandle.OpenExisting)}' is not intercepted",
                e));
            Assert.False(isOpened);

            this.TestWithError(() =>
            {
                if (OperatingSystem.IsWindows())
                {
                    isOpened = EventWaitHandle.TryOpenExisting(name, out EventWaitHandle handle);
                    handle?.Dispose();
                }
            },
            errorChecker: (e) => Assert.StartsWith(
                $"Invoking '{typeof(EventWaitHandle).FullName}.{nameof(EventWaitHandle.TryOpenExisting)}' is not intercepted",
                e));
            Assert.False(isOpened);
        }

#if NET10_0_OR_GREATER
        [Fact(Timeout = 5000)]
        public void TestNamedWaitHandleOptionsOpeningIsReportedAsUncontrolledSynchronization()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            string name = NamedEventProvider.CreateUniqueName();
            using EventWaitHandle existing = NamedEventProvider.CreateNamed(name);
            bool isOpened = false;
            this.TestWithError(() =>
            {
                if (OperatingSystem.IsWindows())
                {
                    using EventWaitHandle handle = EventWaitHandle.OpenExisting(name, default(NamedWaitHandleOptions));
                    isOpened = true;
                }
            },
            errorChecker: (e) => Assert.StartsWith(
                $"Invoking '{typeof(EventWaitHandle).FullName}.{nameof(EventWaitHandle.OpenExisting)}' is not intercepted",
                e));
            Assert.False(isOpened);
        }

        [Fact(Timeout = 5000)]
        public void TestNamedMutexWithOptionsIsReportedAsUncontrolledSynchronization()
        {
            string name = NamedEventProvider.CreateUniqueName();
            this.TestWithError(() =>
            {
                using var mutex = new Mutex(false, name, default(NamedWaitHandleOptions));
            },
            errorChecker: (e) => Assert.StartsWith($"Invoking '{typeof(Mutex).FullName}..ctor' is not intercepted", e));
        }

        [Fact(Timeout = 5000)]
        public void TestNamedSemaphoreWithOptionsIsReportedAsUncontrolledSynchronization()
        {
            string name = NamedEventProvider.CreateUniqueName();
            this.TestWithError(() =>
            {
                using var semaphore = new Semaphore(0, 1, name, default(NamedWaitHandleOptions));
            },
            errorChecker: (e) => Assert.StartsWith($"Invoking '{typeof(Semaphore).FullName}..ctor' is not intercepted", e));
        }
#endif

        private static EventWaitHandle CreateEvent(int overload, string name) => overload switch
        {
            0 => new EventWaitHandle(false, EventResetMode.AutoReset, name),
            1 => new EventWaitHandle(false, EventResetMode.AutoReset, name, out _),
#if NET10_0_OR_GREATER
            2 => new EventWaitHandle(false, EventResetMode.AutoReset, name, default(NamedWaitHandleOptions)),
            3 => new EventWaitHandle(false, EventResetMode.AutoReset, name, default(NamedWaitHandleOptions), out _),
#endif
            _ => throw new ArgumentOutOfRangeException(nameof(overload))
        };
#endif
    }
}
