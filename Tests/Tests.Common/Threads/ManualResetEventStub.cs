// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;

namespace Microsoft.Coyote.Tests.Common.Threads
{
    internal class ManualResetEventStub : IDisposable
    {
        private readonly ManualResetEvent Handle;

        internal ManualResetEventStub(bool initialState)
        {
            this.Handle = new ManualResetEvent(initialState);
        }

        internal void Set() => this.Handle.Set();
        internal void Reset() => this.Handle.Reset();
        internal bool WaitOne() => this.Handle.WaitOne();

        public void Dispose() => this.Handle.Dispose();
    }
}
