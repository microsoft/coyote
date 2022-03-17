// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Interface that provides logic for managing a <see cref="Server"/> instance.
    /// </summary>
    public interface IServerManager
    {
        /// <summary>
        /// The id of the managed server.
        /// </summary>
        string ServerId { get; }

        /// <summary>
        /// Collection of all remote server ids.
        /// </summary>
        IEnumerable<string> RemoteServerIds { get; }

        /// <summary>
        /// Total number of servers in the service.
        /// </summary>
        int NumServers { get; }

        /// <summary>
        /// The leader election due time.
        /// </summary>
        TimeSpan LeaderElectionDueTime { get; }

        /// <summary>
        /// The leader election periodic time interval.
        /// </summary>
        TimeSpan LeaderElectionPeriod { get; }

        /// <summary>
        /// The number of times to ignore HandleTimeout
        /// </summary>
        int TimeoutDelay { get; }

        /// <summary>
        /// Initialize the server.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Start the server.
        /// </summary>
        void Start();

        /// <summary>
        /// Notifies the manager that the server was elected as leader.
        /// </summary>
        void NotifyElectedLeader(int term);
    }
}
