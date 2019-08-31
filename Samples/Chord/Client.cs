using System.Collections.Generic;
using Microsoft.Coyote;

namespace Chord
{
    internal class Client : Machine
    {
        #region events

        internal class Config : Event
        {
            public MachineId ClusterManager;
            public List<int> Keys;

            public Config(MachineId clusterManager, List<int> keys)
                : base()
            {
                this.ClusterManager = clusterManager;
                this.Keys = keys;
            }
        }

        private class Local : Event { }

        #endregion

        #region fields

        MachineId ClusterManager;

        List<int> Keys;
        int QueryCounter;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(Querying))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.ClusterManager = (this.ReceivedEvent as Config).ClusterManager;
            this.Keys = (this.ReceivedEvent as Config).Keys;

            // LIVENESS BUG: can never detect the key, and keeps looping without
            // exiting the process. Enable to introduce the bug.
            this.Keys.Add(17);

            this.QueryCounter = 0;

            this.Raise(new Local());
        }

        [OnEntry(nameof(QueryingOnEntry))]
        [OnEventGotoState(typeof(Local), typeof(Waiting))]
        class Querying : MachineState { }

        void QueryingOnEntry()
        {
            if (this.QueryCounter < 5)
            {
                if (this.Random())
                {
                    var key = this.GetNextQueryKey();
                    this.Logger.WriteLine($"<ChordLog> Client is searching for successor of key '{key}'.");
                    this.Send(this.ClusterManager, new ChordNode.FindSuccessor(this.Id, key));
                    this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientRequest(key));
                }
                else if (this.Random())
                {
                    this.Send(this.ClusterManager, new ClusterManager.CreateNewNode());
                }
                else
                {
                    this.Send(this.ClusterManager, new ClusterManager.TerminateNode());
                }

                this.QueryCounter++;
            }

            this.Raise(new Local());
        }

        int GetNextQueryKey()
        {
            int keyIndex = -1;
            while (keyIndex < 0)
            {
                for (int i = 0; i < this.Keys.Count; i++)
                {
                    if (this.Random())
                    {
                        keyIndex = i;
                        break;
                    }
                }
            }

            return this.Keys[keyIndex];
        }

        [OnEventGotoState(typeof(Local), typeof(Querying))]
        [OnEventDoAction(typeof(ChordNode.FindSuccessorResp), nameof(ProcessFindSuccessorResp))]
        [OnEventDoAction(typeof(ChordNode.QueryIdResp), nameof(ProcessQueryIdResp))]
        class Waiting : MachineState { }

        void ProcessFindSuccessorResp()
        {
            var successor = (this.ReceivedEvent as ChordNode.FindSuccessorResp).Node;
            var key = (this.ReceivedEvent as ChordNode.FindSuccessorResp).Key;
            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientResponse(key));
            this.Send(successor, new ChordNode.QueryId(this.Id));
        }

        void ProcessQueryIdResp()
        {
            this.Raise(new Local());
        }

        #endregion
    }
}
