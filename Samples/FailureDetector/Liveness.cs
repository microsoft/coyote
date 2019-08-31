using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace FailureDetector
{
    /// <summary>
    /// In addition to safety specifications, Coyote also allows programmers
    /// to express liveness specifications such as absence of deadlocks
    /// and livelocks in the test program, using a liveness monitor.
    ///
    /// This monitor starts in the state 'Init' and transitions to the
    /// state 'Wait' upon receiving the event 'RegisterNodes', which
    /// contains references to all nodes in the program.
    ///
    /// Whenever the 'Driver' machine receives a 'NodeFailed' event from
    /// the 'FailureDetector' machine, it forwards that event to the
    /// this monitor which then removes the machine whose failure was
    /// detected from the set of nodes.
    ///
    /// The monitor exits the 'Hot' 'Init' state only when all nodes becomes
    /// empty, i.e., when the failure of all node machines has been detected.
    /// Thus, this monitor expresses the specification that failure of
    /// every node machine must be eventually detected.
    ///
    /// Read the wiki (https://github.com/Microsoft/Coyote/wiki) to learn more
    /// about liveness checking in Coyote.
    /// </summary>
    internal class Liveness : Monitor
    {
        internal class RegisterNodes : Event
        {
            public HashSet<MachineId> Nodes;

            public RegisterNodes(HashSet<MachineId> nodes)
            {
                this.Nodes = nodes;
            }
        }

        HashSet<MachineId> Nodes;

        [Start]
        [OnEventDoAction(typeof(RegisterNodes), nameof(RegisterNodesAction))]
        class Init : MonitorState { }

        void RegisterNodesAction()
        {
            var nodes = (this.ReceivedEvent as RegisterNodes).Nodes;
            this.Nodes = new HashSet<MachineId>(nodes);
            this.Goto<Wait>();
        }

        /// <summary>
        /// A hot state denotes that the liveness property is not
        /// currently satisfied.
        /// </summary>
        [Hot]
        [OnEventDoAction(typeof(FailureDetector.NodeFailed), nameof(NodeDownAction))]
        class Wait : MonitorState { }

        void NodeDownAction()
        {
            var node = (this.ReceivedEvent as FailureDetector.NodeFailed).Node;
            this.Nodes.Remove(node);
            if (this.Nodes.Count == 0)
            {
                // When the liveness property has been satisfied
                // transition out of the hot state.
                this.Goto<Done>();
            }
        }

        class Done : MonitorState { }
    }
}
