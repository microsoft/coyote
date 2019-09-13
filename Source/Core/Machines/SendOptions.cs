// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// Represents a send event configuration that is used during testing.
    /// </summary>
    public class SendOptions
    {
        /// <summary>
        /// The default send options.
        /// </summary>
        public static SendOptions Default { get; } = new SendOptions();

        /// <summary>
        /// True if this event must always be handled, else false.
        /// </summary>
        public bool MustHandle { get; private set; }

        /// <summary>
        /// Specifies that there must not be more than N instances of the
        /// event in the inbox queue of the receiver machine.
        /// </summary>
        public int Assert { get; private set; }

        /// <summary>
        /// Speciﬁes that during testing, an execution that increases the cardinality of the
        /// event beyond N in the receiver machine inbox queue must not be generated.
        /// </summary>
        public int Assume { get; private set; }

        /// <summary>
        /// User-defined hash of the event payload. The default value is 0. Set it to a custom value
        /// to improve the accuracy of liveness checking when state-caching is enabled.
        /// </summary>
        public int HashedState { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SendOptions"/> class.
        /// </summary>
        public SendOptions(bool mustHandle = false, int assert = -1, int assume = -1, int hashedState = 0)
        {
            this.MustHandle = mustHandle;
            this.Assert = assert;
            this.Assume = assume;
            this.HashedState = hashedState;
        }

        /// <summary>
        /// A string that represents the current options.
        /// </summary>
        public override string ToString() =>
            string.Format("SendOptions[MustHandle='{0}', Assert='{1}', Assume='{2}', HashedState='{3}']",
                this.MustHandle, this.Assert, this.Assume, this.HashedState);
    }
}
