// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET
using System;
using System.Threading;

namespace Microsoft.Coyote.Tests.Common.Threading
{
    /// <summary>
    /// Helper class that inspects named system events with the uncontrolled runtime.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose, so that tests can observe the system events that
    /// exist, regardless of how the code under test is rewritten.
    /// </remarks>
    public static class NamedEventProvider
    {
        /// <summary>
        /// Returns a unique name for a system event that does not exist yet.
        /// </summary>
        public static string CreateUniqueName() => $"Coyote.Tests.{Guid.NewGuid():N}";

        /// <summary>
        /// Checks if a named system event exists, which is only observable on Windows.
        /// </summary>
        public static bool Exists(string name)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("Named system events can only be observed on Windows.");
            }

            if (EventWaitHandle.TryOpenExisting(name, out EventWaitHandle handle))
            {
                handle.Dispose();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a named system event on Windows, which the caller must dispose.
        /// </summary>
        public static EventWaitHandle CreateNamed(string name)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("Named system events can only be created on Windows.");
            }

            return new EventWaitHandle(false, EventResetMode.ManualReset, name);
        }

        /// <summary>
        /// Checks if events created with the specified name are shared, by creating two events with the
        /// same name and checking whether signaling one signals the other.
        /// </summary>
        public static bool IsShared(string name)
        {
            using var first = new EventWaitHandle(false, EventResetMode.ManualReset, name, out _);
            using var second = new EventWaitHandle(false, EventResetMode.ManualReset, name, out bool secondCreatedNew);
            first.Set();
            return !secondCreatedNew || second.WaitOne(0);
        }
    }
}
#endif
