// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The pop state event.
    /// </summary>
    [DataContract]
    internal sealed class PopStateEvent : Event
    {
        /// <summary>
        /// Gets a <see cref="PopStateEvent"/> instance.
        /// </summary>
        internal static PopStateEvent Instance { get; } = new PopStateEvent();

        /// <summary>
        /// Initializes a new instance of the <see cref="PopStateEvent"/> class.
        /// </summary>
        private PopStateEvent()
            : base()
        {
        }
    }
}
