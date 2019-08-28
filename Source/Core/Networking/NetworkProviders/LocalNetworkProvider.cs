// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Coyote.Net
{
    /// <summary>
    /// The local network provider.
    /// </summary>
    internal class LocalNetworkProvider : INetworkProvider
    {
        /// <summary>
        /// Instance of the runtime.
        /// </summary>
        private readonly ICoyoteRuntime Runtime;

        /// <summary>
        /// The local endpoint.
        /// </summary>
        private readonly string LocalEndpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalNetworkProvider"/> class.
        /// </summary>
        public LocalNetworkProvider(ICoyoteRuntime runtime)
        {
            this.Runtime = runtime;
            this.LocalEndpoint = string.Empty;
        }

        /// <summary>
        /// Creates a new remote machine of the specified type
        /// and with the specified event. An optional friendly
        /// name can be specified. If the friendly name is null
        /// or the empty string, a default value will be given.
        /// </summary>
        MachineId INetworkProvider.RemoteCreateMachine(Type type, string friendlyName,
            string endpoint, Event e)
        {
            return this.Runtime.CreateMachine(type, friendlyName, e);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        void INetworkProvider.RemoteSend(MachineId target, Event e)
        {
            this.Runtime.SendEvent(target, e);
        }

        /// <summary>
        /// Returns the local endpoint.
        /// </summary>
        string INetworkProvider.GetLocalEndpoint()
        {
            return this.LocalEndpoint;
        }

        /// <summary>
        /// Disposes the network provider.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
