// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Coyote
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
