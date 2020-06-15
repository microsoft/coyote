// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// This is a special operation that is completed when the Actor returns to a quiescent state,
    /// meaning the inbox cannot process any events, either it is empty or everything in it is
    /// deferred.  Quiescence is never reached on a StateMachine that has a DefaultEvent handler.
    /// In that case the default event handler can be considered a quiescent notification.
    /// </summary>
    public class QuiescentOperation : AwaitableOperation<bool>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuiescentOperation"/> class.
        /// </summary>
        /// <param name="id">The id for this operation (defaults to Guid.Empty).</param>
        public QuiescentOperation(Guid id = default)
        {
            this.Id = id;
        }
    }
}
