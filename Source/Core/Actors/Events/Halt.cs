// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The halt event.
    /// </summary>
    [DataContract]
    public sealed class Halt : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Halt"/> class.
        /// </summary>
        public Halt()
            : base()
        {
        }
    }
}
