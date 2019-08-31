using System.Collections.Generic;
using Microsoft.Coyote;

namespace FailureDetector
{
    /// <summary>
    /// Implementation of a failure detector Coyote machine.
    /// </summary>
    internal class FailureDetector : Machine
    {
        internal class Config : Event
        {
            public HashSet<MachineId> Nodes;

            public Config(HashSet<MachineId> nodes)
            {
                this.Nodes = nodes;
            }
        }

        internal class NodeFailed : Event
        {
            public MachineId Node;

            public NodeFailed(MachineId node)
            {
                this.Node = node;
            }
        }

        class TimerCancelled : Event { }
        class RoundDone : Event { }
        class Unit : Event { }

        /// <summary>
        /// Nodes to be monitored.
        /// </summary>
        HashSet<MachineId> Nodes;

        /// <summary>
        /// Set of registered clients.
        /// </summary>
        HashSet<MachineId> Clients;

        /// <summary>
        /// Number of made 'Ping' attempts.
        /// </summary>
		int Attempts;

        /// <summary>
        /// Set of alive nodes.
        /// </summary>
        HashSet<MachineId> Alive;

        /// <summary>
        /// Collected responses in one round.
        /// </summary>
        HashSet<MachineId> Responses;

        /// <summary>
        /// Reference to the timer machine.
        /// </summary>
        MachineId Timer;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(Driver.RegisterClient), nameof(RegisterClientAction))]
        [OnEventDoAction(typeof(Driver.UnregisterClient), nameof(UnregisterClientAction))]
        [OnEventPushState(typeof(Unit), typeof(SendPing))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            var nodes = (this.ReceivedEvent as Config).Nodes;

            this.Nodes = new HashSet<MachineId>(nodes);
            this.Clients = new HashSet<MachineId>();
            this.Alive = new HashSet<MachineId>();
            this.Responses = new HashSet<MachineId>();

            // Initializes the alive set to contain all available nodes.
            foreach (var node in this.Nodes)
            {
                this.Alive.Add(node);
            }

            // Initializes the timer.
            this.Timer = this.CreateMachine(typeof(Timer), new Timer.Config(this.Id));

            // Transitions to the 'SendPing' state after everything has initialized.
            this.Raise(new Unit());
        }

        void RegisterClientAction()
        {
            var client = (this.ReceivedEvent as Driver.RegisterClient).Client;
            this.Clients.Add(client);
        }

        void UnregisterClientAction()
        {
            var client = (this.ReceivedEvent as Driver.UnregisterClient).Client;
            if (this.Clients.Contains(client))
            {
                this.Clients.Remove(client);
            }
        }

        [OnEntry(nameof(SendPingOnEntry))]
        [OnEventGotoState(typeof(RoundDone), typeof(Reset))]
        [OnEventPushState(typeof(TimerCancelled), typeof(WaitForCancelResponse))]
        [OnEventDoAction(typeof(Node.Pong), nameof(PongAction))]
        [OnEventDoAction(typeof(Timer.TimeoutEvent), nameof(TimeoutAction))]
        class SendPing : MachineState { }

        void SendPingOnEntry()
        {
            foreach (var node in this.Nodes)
            {
                // Sends a 'Ping' event to any machine that has not responded.
                if (this.Alive.Contains(node) && !this.Responses.Contains(node))
                {
                    this.Monitor<Safety>(new Safety.Ping(node));
                    this.Send(node, new Node.Ping(this.Id));
                }
            }

            // Starts the timer with a given timeout value. Note that in this sample,
            // the timeout value is not actually used, because the timer is abstracted
            // away using Coyote to enable systematic testing (i.e. timeouts are triggered
            // nondeterministically). In production, this model timer machine will be
            // replaced by a real timer.
            this.Send(this.Timer, new Timer.StartTimerEvent(100));
        }

        /// <summary>
        /// This action is triggered whenever a node replies with a 'Pong' event.
        /// </summary>
        void PongAction()
        {
            var node = (this.ReceivedEvent as Node.Pong).Node;
            if (this.Alive.Contains(node))
            {
                this.Responses.Add(node);

                // Checks if the status of alive nodes has changed.
                if (this.Responses.Count == this.Alive.Count)
                {
                    this.Send(this.Timer, new Timer.CancelTimerEvent());
                    this.Raise(new TimerCancelled());
                }
            }
        }

        void TimeoutAction()
        {
            // One attempt is done for this round.
            this.Attempts++;

            // Each round has a maximum number of 2 attempts.
            if (this.Responses.Count < this.Alive.Count && this.Attempts < 2)
            {
                // Retry by looping back to same state.
                this.Goto<SendPing>();
            }
            else
            {
                foreach (var node in this.Nodes)
                {
                    if (this.Alive.Contains(node) && !this.Responses.Contains(node))
                    {
                        this.Alive.Remove(node);

                        // Send failure notification to any clients.
                        foreach (var client in this.Clients)
                        {
                            this.Send(client, new NodeFailed(node));
                        }
                    }
                }

                this.Raise(new RoundDone());
            }
        }

        [OnEventDoAction(typeof(Timer.CancelSuccess), nameof(CancelSuccessAction))]
        [OnEventDoAction(typeof(Timer.CancelFailure), nameof(CancelFailure))]
        [DeferEvents(typeof(Timer.TimeoutEvent), typeof(Node.Pong))]
        class WaitForCancelResponse : MachineState { }

        void CancelSuccessAction()
        {
            this.Raise(new RoundDone());
        }

        void CancelFailure()
        {
            this.Pop();
        }

        [OnEntry(nameof(ResetOnEntry))]
        [OnEventGotoState(typeof(Timer.TimeoutEvent), typeof(SendPing))]
        [IgnoreEvents(typeof(Node.Pong))]
        class Reset : MachineState { }

        /// <summary>
        /// Prepares the failure detector for the next round.
        /// </summary>
        void ResetOnEntry()
        {
            this.Attempts = 0;
            this.Responses.Clear();

            // Starts the timer with a given timeout value (see details above).
            this.Send(this.Timer, new Timer.StartTimerEvent(1000));
        }
    }
}
