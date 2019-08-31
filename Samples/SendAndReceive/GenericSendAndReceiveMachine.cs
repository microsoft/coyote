using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Coyote;
using Microsoft.Coyote.Runtime;

namespace SendAndReceive
{
    /// <summary>
    /// Generic machine that helps fetch response.
    /// </summary>
    class GetReponseMachine<T> : Machine where T : Event
    {
        /// <summary>
        /// Static method for safely getting a response from a machine.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <param name="mid">Target machine id.</param>
        /// <param name="ev">Event to send whose respose we're interested in getting.</param>
        public static async Task<T> GetResponse(ICoyoteRuntime runtime, MachineId mid, Func<MachineId, Event> ev)
        {
            var conf = new Config(mid, ev);
            // This method awaits until the GetResponseMachine finishes its Execute method
            await runtime.CreateMachineAndExecuteAsync(typeof(GetReponseMachine<T>), conf);
            // Safely return the result back (no race condition here)
            return conf.ReceivedEvent;
        }

        /// <summary>
        /// Internal config event.
        /// </summary>
        class Config : Event
        {
            public MachineId TargetMachineId;
            public Func<MachineId, Event> Ev;
            public T ReceivedEvent;

            public Config(MachineId targetMachineId, Func<MachineId, Event> ev)
            {
                this.TargetMachineId = targetMachineId;
                this.Ev = ev;
                this.ReceivedEvent = null;
            }
        }

        [Start]
        [OnEntry(nameof(Execute))]
        class Init : MachineState { }

        async Task Execute()
        {
            // Grab the config event.
            var config = this.ReceivedEvent as Config;
            // send event to target machine, adding self Id
            this.Send(config.TargetMachineId, config.Ev(this.Id));
            // Wait for the response.
            var rv = await this.Receive(typeof(T));
            // Stash in the shared config event.
            config.ReceivedEvent = rv as T;
            // Finally, halt.
            this.Raise(new Halt());
        }
    }
}
