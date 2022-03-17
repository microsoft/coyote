// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Mock implementation of a ClusterManager that introduces controlled
    /// nondeterminism to exercise the specification that no more than
    /// one leader can be elected in the same term.
    /// </summary>
    public class MockClusterManagerWithFailure : MockClusterManager
    {
        /// <summary>
        /// We override this method to introduce controlled nondeterminism by invoking
        /// <see cref="IActorRuntime.Random"/> method. The returned random values are
        /// controlled by the runtime durig testing and systematically explored with
        /// other combinations of nondeterminism to find bugs.
        /// </summary>
        public override async Task BroadcastVoteRequestAsync(Event e)
        {
            var request = e as VoteRequestEvent;

            foreach (var pair in this.Servers)
            {
                if (request.CandidateId != pair.Key)
                {
                    var server = pair.Value;
                    this.SendEvent(server, request);
                    if (this.RandomBoolean())
                    {
                        // Nondeterministically send a duplicate vote to exercise the corner case
                        // where networking communication sends duplicate messages. This can cause
                        // a Raft server to count duplicate votes, leading to more than one leader
                        // being elected at the same term.
                        this.SendEvent(server, request);
                    }
                }
            }

            await Task.CompletedTask;
        }
    }
}
