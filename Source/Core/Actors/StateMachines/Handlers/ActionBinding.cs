// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors
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
