using Microsoft.Coyote;

namespace TwoPhaseCommit
{
    internal class TwoPhaseCommit : Machine
    {
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            var coordinator = this.CreateMachine(typeof(Coordinator));
            this.Send(coordinator, new Coordinator.Config(2));

            var client1 = this.CreateMachine(typeof(Client));
            this.Send(client1, new Client.Config(coordinator));

            var client2 = this.CreateMachine(typeof(Client));
            this.Send(client2, new Client.Config(coordinator));
        }
    }
}
