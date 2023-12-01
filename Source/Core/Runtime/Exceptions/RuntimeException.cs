// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// An exception that is thrown by the Coyote runtime.
    /// </summary>
    [Serializable]
    [DebuggerStepThrough]
    public class RuntimeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeException"/> class.
        /// </summary>
        internal RuntimeException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeException"/> class.
        /// </summary>
        internal RuntimeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeException"/> class.
        /// </summary>
        internal RuntimeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeException"/> class.
        /// </summary>
#if NET8_0_OR_GREATER
        [Obsolete("This method is obsolete due to BinaryFormatter obsoleted", DiagnosticId = "SYSLIB0051")]
#endif
        protected RuntimeException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
    }
}
