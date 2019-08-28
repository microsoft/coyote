// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Defines an action binding.
    /// </summary>
    internal sealed class ActionBinding : EventActionHandler
    {
        /// <summary>
        /// Name of the action.
        /// </summary>
        public string Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionBinding"/> class.
        /// </summary>
        public ActionBinding(string actionName)
        {
            this.Name = actionName;
        }
    }
}
