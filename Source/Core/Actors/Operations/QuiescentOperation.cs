// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// This is a special operation that is completed when the Actor reaches a quiescent state,
    /// meaning the inbox cannot process any events, either it is empty or everything in it is
    /// deferred.  Quiescence is never reach on a StateMachine that has a DefaultEvent handler.
    /// In that case the default event handler can be considered a quiescent notification.
    /// </summary>
    public class QuiescentOperation : Operation<bool>
    {
    }
}
