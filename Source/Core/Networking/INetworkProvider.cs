// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Coyote.Net
{
    /// <summary>
    /// Interface for a network provider.
    /// </summary>
    public interface INetworkProvider : IDisposable
    {
        /// <summary>
        /// Creates a new remote machine of the specified type
        /// and with the specified event. An optional friendly
        /// name can be specified. If the friendly name is null
        /// or the empty string, a default value will be given.
        /// </summary>
        MachineId RemoteCreateMachine(Type type, string friendlyName, string endpoint, Event e);

        /// <summary>
        /// Sends an event to the specified remote machine.
        /// </summary>
        void RemoteSend(MachineId target, Event e);

        /// <summary>
        /// Returns the local endpoint.
        /// </summary>
        string GetLocalEndpoint();
    }
}
