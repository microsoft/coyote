// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    internal class FuzzingState
    {
        internal HashSet<string> SleepingOperations;
        internal HashSet<string> AwakeOperations;

        internal FuzzingState()
        {
            this.SleepingOperations = new HashSet<string>();
            this.AwakeOperations = new HashSet<string>();
        }

        internal void Snooze(Actor actor)
        {
            this.AwakeOperations.Remove(actor.Id.RLId);
            this.SleepingOperations.Add(actor.Id.RLId);
        }

        internal void Wake(Actor actor)
        {
            this.AwakeOperations.Add(actor.Id.RLId);
            this.SleepingOperations.Remove(actor.Id.RLId);
        }

        internal int GetHashedState(AsyncOperation operation)
        {

        }
    }
}