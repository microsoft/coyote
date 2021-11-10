// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
            if (actor != null)
            {
                this.AwakeOperations.Add(actor.Id.RLId);
                this.SleepingOperations.Remove(actor.Id.RLId);
            }
        }

        internal int GetHashedState(AsyncOperation operation)
        {
            int sleepinghash = 0;
            foreach (string id in this.SleepingOperations)
            {
                sleepinghash -= id.GetHashCode();
            }

            int awakehash = 0;
            foreach (string id in this.AwakeOperations)
            {
                awakehash += id.GetHashCode();
            }

            if (operation != null)
            {
                return sleepinghash + awakehash + operation.GetHashCode();
            }
            else
            {
                return sleepinghash + awakehash;
            }
        }
    }
}
