using System;
using Microsoft.Coyote;

namespace ReplicatingStorage
{
    internal class StorageNode : Machine
    {
        #region events

        /// <summary>
        /// Used to configure the storage node.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public MachineId Environment;
            public MachineId NodeManager;
            public int Id;

            public ConfigureEvent(MachineId env, MachineId manager, int id)
                : base()
            {
                this.Environment = env;
                this.NodeManager = manager;
                this.Id = id;
            }
        }

        public class StoreRequest : Event
        {
            public int Command;

            public StoreRequest(int cmd)
                : base()
            {
                this.Command = cmd;
            }
        }

        public class SyncReport : Event
        {
            public int NodeId;
            public int Data;

            public SyncReport(int id, int data)
                : base()
            {
                this.NodeId = id;
                this.Data = data;
            }
        }

        public class SyncRequest : Event
        {
            public int Data;

            public SyncRequest(int data)
                : base()
            {
                this.Data = data;
            }
        }

        internal class ShutDown : Event { }
        private class LocalEvent : Event { }

        #endregion

        #region fields

        /// <summary>
        /// The environment.
        /// </summary>
        private MachineId Environment;

        /// <summary>
        /// The storage node manager.
        /// </summary>
        private MachineId NodeManager;

        /// <summary>
        /// The storage node id.
        /// </summary>
        private int NodeId;

        /// <summary>
        /// The data that this storage node contains.
        /// </summary>
        private int Data;

        /// <summary>
        /// The sync report timer.
        /// </summary>
        private MachineId SyncTimer;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
        [DeferEvents(typeof(SyncTimer.TimeoutEvent))]
        class Init : MachineState { }

        void EntryOnInit()
        {
            this.Data = 0;
            this.SyncTimer = this.CreateMachine(typeof(SyncTimer));
            this.Send(this.SyncTimer, new SyncTimer.ConfigureEvent(this.Id));
        }

        void Configure()
        {
            this.Environment = (this.ReceivedEvent as ConfigureEvent).Environment;
            this.NodeManager = (this.ReceivedEvent as ConfigureEvent).NodeManager;
            this.NodeId = (this.ReceivedEvent as ConfigureEvent).Id;

            this.Logger.WriteLine("\n [StorageNode-{0}] is up and running.\n", this.NodeId);

            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeCreated(this.NodeId));
            this.Send(this.Environment, new Environment.NotifyNode(this.Id));

            this.Raise(new LocalEvent());
        }

        [OnEventDoAction(typeof(StoreRequest), nameof(Store))]
        [OnEventDoAction(typeof(SyncRequest), nameof(Sync))]
        [OnEventDoAction(typeof(SyncTimer.TimeoutEvent), nameof(GenerateSyncReport))]
        [OnEventDoAction(typeof(Environment.FaultInject), nameof(Terminate))]
        class Active : MachineState { }

        void Store()
        {
            var cmd = (this.ReceivedEvent as StoreRequest).Command;
            this.Data += cmd;
            this.Logger.WriteLine("\n [StorageNode-{0}] is applying command {1}.\n", this.NodeId, cmd);
            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeUpdate(this.NodeId, this.Data));
        }

        void Sync()
        {
            var data = (this.ReceivedEvent as SyncRequest).Data;
            this.Data = data;
            this.Logger.WriteLine("\n [StorageNode-{0}] is syncing with data {1}.\n", this.NodeId, this.Data);
            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeUpdate(this.NodeId, this.Data));
        }

        void GenerateSyncReport()
        {
            this.Send(this.NodeManager, new SyncReport(this.NodeId, this.Data));
        }

        void Terminate()
        {
            this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyNodeFail(this.NodeId));
            this.Send(this.SyncTimer, new Halt());
            this.Raise(new Halt());
        }

        #endregion
    }
}
