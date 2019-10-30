// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The default event.
    /// </summary>
    [DataContract]
    public sealed class Default : Event
    {
        /// <summary>
        /// Gets an instance of the default event.
        /// </summary>
        public static Default Event { get; } = new Default();

        /// <summary>
        /// Initializes a new instance of the <see cref="Default"/> class.
        /// </summary>
        private Default()
            : base()
        {
        }
    }
}
