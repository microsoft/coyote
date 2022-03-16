// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Tests the Raft service implementation by creating, hosting and executing
    /// in-memory the <see cref="Server"/> Coyote state machine instances, as well
    /// as a mock in-memory client.
    /// </summary>
    public class RaftTestScenarioWithFailure : RaftTestScenario
    {
        /// <summary>
        /// Creates a new server host.
        /// </summary>
        protected override ActorId CreateClusterManager(IActorRuntime runtime) =>
            runtime.CreateActor(typeof(MockClusterManagerWithFailure));
    }
}
