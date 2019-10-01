// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides access to the runtime associated with the current execution context.
    /// </summary>
    internal class RuntimeProvider
    {
        /// <summary>
        /// The default executing runtime.
        /// </summary>
        private static readonly CoyoteRuntime Default = new ProductionRuntime(Configuration.Create());

        /// <summary>
        /// The currently executing runtime.
        /// </summary>
        /// <remarks>
        /// This can only be set/get internally today -- in testing, everything is serialized, and before
        /// each iteration, this runtime is set in a local async context, so that should be thread safe.
        /// In production, its a singleton that is only set when this type is loaded in the very beginning.
        /// If we allow the user to override/set it manually, we should have some assertion that its not done
        /// after the runtime "starts", else it could become really expensive to have a lock here to check it
        /// every time we call these properties.
        /// </remarks>
        internal virtual CoyoteRuntime Current => Default;

        /// <summary>
        /// Sets the runtime associated with the current execution context.
        /// </summary>
        internal virtual void SetCurrentRuntime(CoyoteRuntime runtime)
        {
        }
    }
}
