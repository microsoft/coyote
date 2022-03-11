// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET || NETCOREAPP3_1
namespace Microsoft.Coyote.Rewriting.Types.Net.Http
{
    /// <summary>
    /// Header that can be attached to HTTP requests during controlled testing
    /// to propagate runtime related information.
    /// </summary>
    internal static class HttpRequestHeader
    {
        /// <summary>
        /// Header that contains the runtime id associated with a request.
        /// </summary>
        internal const string RuntimeId = "coyote-runtime-id";

        /// <summary>
        /// Header that contains the source operation id associated with a request.
        /// </summary>
        internal const string SourceOperationId = "coyote-source-operation-id";
    }
}
#endif
