// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The default event.
    /// </summary>
    [DataContract]
    public sealed class DefaultEvent : Event
    {
        /// <summary>
        /// Gets an instance of the default event.
        /// </summary>
        public static DefaultEvent Instance { get; } = new DefaultEvent();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultEvent"/> class.
        /// </summary>
        private DefaultEvent()
            : base()
        {
        }
    }
}
