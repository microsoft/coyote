// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides access to the runtime associated with the current asynchronous control flow.
    /// </summary>
    internal class AsyncLocalRuntimeProvider : RuntimeProvider
    {
        /// <summary>
        /// Stores the runtime executing an asynchronous control flow.
        /// </summary>
        private static readonly AsyncLocal<MachineRuntime> AsyncLocalRuntime = new AsyncLocal<MachineRuntime>();

        /// <summary>
        /// The currently executing runtime.
        /// </summary>
        internal override MachineRuntime Current => AsyncLocalRuntime.Value ??
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                "Uncontrolled task with id '{0}' tried to access the runtime. Please make sure to avoid using concurrency " +
                "APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside machine handlers or controlled tasks. If you " +
                "are using external libraries that are executing concurrently, you will need to mock them during testing.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>"));

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLocalRuntimeProvider"/> class.
        /// </summary>
        internal AsyncLocalRuntimeProvider(MachineRuntime runtime)
            : base()
        {
            this.SetCurrentRuntime(runtime);
        }

        /// <summary>
        /// Sets the runtime associated with the current execution context.
        /// </summary>
        internal override void SetCurrentRuntime(MachineRuntime runtime) => AsyncLocalRuntime.Value = runtime;
    }
}
