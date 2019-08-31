using Microsoft.Coyote;

namespace PingPong.AsyncAwait
{
    /// <summary>
    /// This machine acts as a test harness. It models a network environment,
    /// by creating a 'Server' and a 'Client' machine.
    /// </summary>
    internal class NetworkEnvironment : Machine
    {
        /// <summary>
        /// Each Coyote machine declares one or more machine states (or simply states).
        ///
        /// One of the states must be declared as the initial state using the 'Start'
        /// attribute. When the machine gets constructed, it will transition the
        /// declared initial state.
        [Start]
        ///
        /// A Coyote machine state can declare one or more action. This state declares an
        /// 'OnEntry' action, which executes the 'InitOnEntry' method when the machine
        /// transitions to the 'Init' state. Only one 'OnEntry' action can be declared
        /// per machine state.
        [OnEntry(nameof(InitOnEntry))]
        /// </summary>
        class Init : MachineState { }

        /// <summary>
        /// An action that executes (asynchronously) when the 'NetworkEnvironment' machine
        /// transitions to the 'Init' state.
        /// </summary>
        void InitOnEntry()
        {
            // Creates (asynchronously) a server machine.
            var server = this.CreateMachine(typeof(Server));
            // Creates (asynchronously) a client machine, and passes the
            // 'Config' event as payload. 'Config' contains a reference
            // to the server machine.
            this.CreateMachine(typeof(Client), new Client.Config(server));
        }
    }
}