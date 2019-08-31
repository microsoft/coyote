using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace Raft
{
    /// <summary>
    /// A server in Raft can be one of the following three roles:
    /// follower, candidate or leader.
    /// </summary>
    internal class Server : Machine
    {
        #region events

        /// <summary>
        /// Used to configure the server.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public int Id;
            public MachineId[] Servers;
            public MachineId ClusterManager;

            public ConfigureEvent(int id, MachineId[] servers, MachineId manager)
                : base()
            {
                this.Id = id;
                this.Servers = servers;
                this.ClusterManager = manager;
            }
        }

        /// <summary>
        /// Initiated by candidates during elections.
        /// </summary>
        public class VoteRequest : Event
        {
            public int Term; // candidate’s term
            public MachineId CandidateId; // candidate requesting vote
            public int LastLogIndex; // index of candidate’s last log entry
            public int LastLogTerm; // term of candidate’s last log entry

            public VoteRequest(int term, MachineId candidateId, int lastLogIndex, int lastLogTerm)
                : base()
            {
                this.Term = term;
                this.CandidateId = candidateId;
                this.LastLogIndex = lastLogIndex;
                this.LastLogTerm = lastLogTerm;
            }
        }

        /// <summary>
        /// Response to a vote request.
        /// </summary>
        public class VoteResponse : Event
        {
            public int Term; // currentTerm, for candidate to update itself
            public bool VoteGranted; // true means candidate received vote

            public VoteResponse(int term, bool voteGranted)
                : base()
            {
                this.Term = term;
                this.VoteGranted = voteGranted;
            }
        }

        /// <summary>
        /// Initiated by leaders to replicate log entries and
        /// to provide a form of heartbeat.
        /// </summary>
        public class AppendEntriesRequest : Event
        {
            public int Term; // leader's term
            public MachineId LeaderId; // so follower can redirect clients
            public int PrevLogIndex; // index of log entry immediately preceding new ones
            public int PrevLogTerm; // term of PrevLogIndex entry
            public List<Log> Entries; // log entries to store (empty for heartbeat; may send more than one for efficiency)
            public int LeaderCommit; // leader’s CommitIndex

            public MachineId ReceiverEndpoint; // client

            public AppendEntriesRequest(int term, MachineId leaderId, int prevLogIndex,
                int prevLogTerm, List<Log> entries, int leaderCommit, MachineId client)
                : base()
            {
                this.Term = term;
                this.LeaderId = leaderId;
                this.PrevLogIndex = prevLogIndex;
                this.PrevLogTerm = prevLogTerm;
                this.Entries = entries;
                this.LeaderCommit = leaderCommit;
                this.ReceiverEndpoint = client;
            }
        }

        /// <summary>
        /// Response to an append entries request.
        /// </summary>
        public class AppendEntriesResponse : Event
        {
            public int Term; // current Term, for leader to update itself
            public bool Success; // true if follower contained entry matching PrevLogIndex and PrevLogTerm

            public MachineId Server;
            public MachineId ReceiverEndpoint; // client

            public AppendEntriesResponse(int term, bool success, MachineId server, MachineId client)
                : base()
            {
                this.Term = term;
                this.Success = success;
                this.Server = server;
                this.ReceiverEndpoint = client;
            }
        }

        // Events for transitioning a server between roles.
        private class BecomeFollower : Event { }
        private class BecomeCandidate : Event { }
        private class BecomeLeader : Event { }

        internal class ShutDown : Event { }

        #endregion

        #region fields

        /// <summary>
        /// The id of this server.
        /// </summary>
        int ServerId;

        /// <summary>
        /// The cluster manager machine.
        /// </summary>
        MachineId ClusterManager;

        /// <summary>
        /// The servers.
        /// </summary>
        MachineId[] Servers;

        /// <summary>
        /// Leader id.
        /// </summary>
        MachineId LeaderId;

        /// <summary>
        /// The election timer of this server.
        /// </summary>
        MachineId ElectionTimer;

        /// <summary>
        /// The periodic timer of this server.
        /// </summary>
        MachineId PeriodicTimer;

        /// <summary>
        /// Latest term server has seen (initialized to 0 on
        /// first boot, increases monotonically).
        /// </summary>
        int CurrentTerm;

        /// <summary>
        /// Candidate id that received vote in current term (or null if none).
        /// </summary>
        MachineId VotedFor;

        /// <summary>
        /// Log entries.
        /// </summary>
        List<Log> Logs;

        /// <summary>
        /// Index of highest log entry known to be committed (initialized
        /// to 0, increases monotonically).
        /// </summary>
        int CommitIndex;

        /// <summary>
        /// Index of highest log entry applied to state machine (initialized
        /// to 0, increases monotonically).
        /// </summary>
        int LastApplied;

        /// <summary>
        /// For each server, index of the next log entry to send to that
        /// server (initialized to leader last log index + 1).
        /// </summary>
        Dictionary<MachineId, int> NextIndex;

        /// <summary>
        /// For each server, index of highest log entry known to be replicated
        /// on server (initialized to 0, increases monotonically).
        /// </summary>
        Dictionary<MachineId, int> MatchIndex;

        /// <summary>
        /// Number of received votes.
        /// </summary>
        int VotesReceived;

        /// <summary>
        /// The latest client request.
        /// </summary>
        Client.Request LastClientRequest;

        #endregion

        #region initialization

        [Start]
        [OnEntry(nameof(EntryOnInit))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        [DeferEvents(typeof(VoteRequest), typeof(AppendEntriesRequest))]
        class Init : MachineState { }

        void EntryOnInit()
        {
            this.CurrentTerm = 0;

            this.LeaderId = null;
            this.VotedFor = null;

            this.Logs = new List<Log>();

            this.CommitIndex = 0;
            this.LastApplied = 0;

            this.NextIndex = new Dictionary<MachineId, int>();
            this.MatchIndex = new Dictionary<MachineId, int>();
        }

        void Configure()
        {
            this.ServerId = (this.ReceivedEvent as ConfigureEvent).Id;
            this.Servers = (this.ReceivedEvent as ConfigureEvent).Servers;
            this.ClusterManager = (this.ReceivedEvent as ConfigureEvent).ClusterManager;

            this.ElectionTimer = this.CreateMachine(typeof(ElectionTimer));
            this.Send(this.ElectionTimer, new ElectionTimer.ConfigureEvent(this.Id));

            this.PeriodicTimer = this.CreateMachine(typeof(PeriodicTimer));
            this.Send(this.PeriodicTimer, new PeriodicTimer.ConfigureEvent(this.Id));

            this.Raise(new BecomeFollower());
        }

        #endregion

        #region follower

        [OnEntry(nameof(FollowerOnInit))]
        [OnEventDoAction(typeof(Client.Request), nameof(RedirectClientRequest))]
        [OnEventDoAction(typeof(VoteRequest), nameof(VoteAsFollower))]
        [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsFollower))]
        [OnEventDoAction(typeof(AppendEntriesRequest), nameof(AppendEntriesAsFollower))]
        [OnEventDoAction(typeof(AppendEntriesResponse), nameof(RespondAppendEntriesAsFollower))]
        [OnEventDoAction(typeof(ElectionTimer.TimeoutEvent), nameof(StartLeaderElection))]
        [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        [OnEventGotoState(typeof(BecomeCandidate), typeof(Candidate))]
        [IgnoreEvents(typeof(PeriodicTimer.TimeoutEvent))]
        class Follower : MachineState { }

        void FollowerOnInit()
        {
            this.LeaderId = null;
            this.VotesReceived = 0;

            this.Send(this.ElectionTimer, new ElectionTimer.StartTimerEvent());
        }

        void RedirectClientRequest()
        {
            if (this.LeaderId != null)
            {
                this.Send(this.LeaderId, this.ReceivedEvent);
            }
            else
            {
                this.Send(this.ClusterManager, new ClusterManager.RedirectRequest(this.ReceivedEvent));
            }
        }

        void StartLeaderElection()
        {
            this.Raise(new BecomeCandidate());
        }

        void VoteAsFollower()
        {
            var request = this.ReceivedEvent as VoteRequest;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;
            }

            this.Vote(this.ReceivedEvent as VoteRequest);
        }

        void RespondVoteAsFollower()
        {
            var request = this.ReceivedEvent as VoteResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;
            }
        }

        void AppendEntriesAsFollower()
        {
            var request = this.ReceivedEvent as AppendEntriesRequest;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;
            }

            this.AppendEntries(this.ReceivedEvent as AppendEntriesRequest);
        }

        void RespondAppendEntriesAsFollower()
        {
            var request = this.ReceivedEvent as AppendEntriesResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;
            }
        }

        #endregion

        #region candidate

        [OnEntry(nameof(CandidateOnInit))]
        [OnEventDoAction(typeof(Client.Request), nameof(RedirectClientRequest))]
        [OnEventDoAction(typeof(VoteRequest), nameof(VoteAsCandidate))]
        [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsCandidate))]
        [OnEventDoAction(typeof(AppendEntriesRequest), nameof(AppendEntriesAsCandidate))]
        [OnEventDoAction(typeof(AppendEntriesResponse), nameof(RespondAppendEntriesAsCandidate))]
        [OnEventDoAction(typeof(ElectionTimer.TimeoutEvent), nameof(StartLeaderElection))]
        [OnEventDoAction(typeof(PeriodicTimer.TimeoutEvent), nameof(BroadcastVoteRequests))]
        [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
        [OnEventGotoState(typeof(BecomeLeader), typeof(Leader))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        [OnEventGotoState(typeof(BecomeCandidate), typeof(Candidate))]
        class Candidate : MachineState { }

        void CandidateOnInit()
        {
            this.CurrentTerm++;
            this.VotedFor = this.Id;
            this.VotesReceived = 1;

            this.Send(this.ElectionTimer, new ElectionTimer.StartTimerEvent());

            this.Logger.WriteLine("\n [Candidate] " + this.ServerId + " | term " + this.CurrentTerm +
                " | election votes " + this.VotesReceived + " | log " + this.Logs.Count + "\n");

            this.BroadcastVoteRequests();
        }

        void BroadcastVoteRequests()
        {
            // BUG: duplicate votes from same follower
            this.Send(this.PeriodicTimer, new PeriodicTimer.StartTimerEvent());

            for (int idx = 0; idx < this.Servers.Length; idx++)
            {
                if (idx == this.ServerId)
                    continue;

                var lastLogIndex = this.Logs.Count;
                var lastLogTerm = this.GetLogTermForIndex(lastLogIndex);

                this.Send(this.Servers[idx], new VoteRequest(this.CurrentTerm, this.Id,
                    lastLogIndex, lastLogTerm));
            }
        }

        void VoteAsCandidate()
        {
            var request = this.ReceivedEvent as VoteRequest;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;
                this.Vote(this.ReceivedEvent as VoteRequest);
                this.Raise(new BecomeFollower());
            }
            else
            {
                this.Vote(this.ReceivedEvent as VoteRequest);
            }
        }

        void RespondVoteAsCandidate()
        {
            var request = this.ReceivedEvent as VoteResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;
                this.Raise(new BecomeFollower());
                return;
            }
            else if (request.Term != this.CurrentTerm)
            {
                return;
            }

            if (request.VoteGranted)
            {
                this.VotesReceived++;
                if (this.VotesReceived >= (this.Servers.Length / 2) + 1)
                {
                    this.Logger.WriteLine("\n [Leader] " + this.ServerId + " | term " + this.CurrentTerm +
                        " | election votes " + this.VotesReceived + " | log " + this.Logs.Count + "\n");
                    this.VotesReceived = 0;
                    this.Raise(new BecomeLeader());
                }
            }
        }

        void AppendEntriesAsCandidate()
        {
            var request = this.ReceivedEvent as AppendEntriesRequest;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;
                this.AppendEntries(this.ReceivedEvent as AppendEntriesRequest);
                this.Raise(new BecomeFollower());
            }
            else
            {
                this.AppendEntries(this.ReceivedEvent as AppendEntriesRequest);
            }
        }

        void RespondAppendEntriesAsCandidate()
        {
            var request = this.ReceivedEvent as AppendEntriesResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;
                this.Raise(new BecomeFollower());
            }
        }

        #endregion

        #region leader

        [OnEntry(nameof(LeaderOnInit))]
        [OnEventDoAction(typeof(Client.Request), nameof(ProcessClientRequest))]
        [OnEventDoAction(typeof(VoteRequest), nameof(VoteAsLeader))]
        [OnEventDoAction(typeof(VoteResponse), nameof(RespondVoteAsLeader))]
        [OnEventDoAction(typeof(AppendEntriesRequest), nameof(AppendEntriesAsLeader))]
        [OnEventDoAction(typeof(AppendEntriesResponse), nameof(RespondAppendEntriesAsLeader))]
        [OnEventDoAction(typeof(ShutDown), nameof(ShuttingDown))]
        [OnEventGotoState(typeof(BecomeFollower), typeof(Follower))]
        [IgnoreEvents(typeof(ElectionTimer.TimeoutEvent), typeof(PeriodicTimer.TimeoutEvent))]
        class Leader : MachineState { }

        void LeaderOnInit()
        {
            this.Monitor<SafetyMonitor>(new SafetyMonitor.NotifyLeaderElected(this.CurrentTerm));
            this.Send(this.ClusterManager, new ClusterManager.NotifyLeaderUpdate(this.Id, this.CurrentTerm));

            var logIndex = this.Logs.Count;
            var logTerm = this.GetLogTermForIndex(logIndex);

            this.NextIndex.Clear();
            this.MatchIndex.Clear();
            for (int idx = 0; idx < this.Servers.Length; idx++)
            {
                if (idx == this.ServerId)
                    continue;
                this.NextIndex.Add(this.Servers[idx], logIndex + 1);
                this.MatchIndex.Add(this.Servers[idx], 0);
            }

            for (int idx = 0; idx < this.Servers.Length; idx++)
            {
                if (idx == this.ServerId)
                    continue;
                this.Send(this.Servers[idx], new AppendEntriesRequest(this.CurrentTerm, this.Id,
                    logIndex, logTerm, new List<Log>(), this.CommitIndex, null));
            }
        }

        void ProcessClientRequest()
        {
            this.LastClientRequest = this.ReceivedEvent as Client.Request;

            var log = new Log(this.CurrentTerm, this.LastClientRequest.Command);
            this.Logs.Add(log);

            this.BroadcastLastClientRequest();
        }

        void BroadcastLastClientRequest()
        {
            this.Logger.WriteLine("\n [Leader] " + this.ServerId + " sends append requests | term " +
                this.CurrentTerm + " | log " + this.Logs.Count + "\n");

            var lastLogIndex = this.Logs.Count;

            this.VotesReceived = 1;
            for (int idx = 0; idx < this.Servers.Length; idx++)
            {
                if (idx == this.ServerId)
                    continue;

                var server = this.Servers[idx];
                if (lastLogIndex < this.NextIndex[server])
                    continue;

                var logs = this.Logs.GetRange(this.NextIndex[server] - 1,
                    this.Logs.Count - (this.NextIndex[server] - 1));

                var prevLogIndex = this.NextIndex[server] - 1;
                var prevLogTerm = this.GetLogTermForIndex(prevLogIndex);

                this.Send(server, new AppendEntriesRequest(this.CurrentTerm, this.Id, prevLogIndex,
                    prevLogTerm, logs, this.CommitIndex, this.LastClientRequest.Client));
            }
        }

        void VoteAsLeader()
        {
            var request = this.ReceivedEvent as VoteRequest;

            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;

                this.RedirectLastClientRequestToClusterManager();
                this.Vote(this.ReceivedEvent as VoteRequest);

                this.Raise(new BecomeFollower());
            }
            else
            {
                this.Vote(this.ReceivedEvent as VoteRequest);
            }
        }

        void RespondVoteAsLeader()
        {
            var request = this.ReceivedEvent as VoteResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;

                this.RedirectLastClientRequestToClusterManager();
                this.Raise(new BecomeFollower());
            }
        }

        void AppendEntriesAsLeader()
        {
            var request = this.ReceivedEvent as AppendEntriesRequest;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;

                this.RedirectLastClientRequestToClusterManager();
                this.AppendEntries(this.ReceivedEvent as AppendEntriesRequest);

                this.Raise(new BecomeFollower());
            }
        }

        void RespondAppendEntriesAsLeader()
        {
            var request = this.ReceivedEvent as AppendEntriesResponse;
            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = null;

                this.RedirectLastClientRequestToClusterManager();
                this.Raise(new BecomeFollower());
                return;
            }
            else if (request.Term != this.CurrentTerm)
            {
                return;
            }

            if (request.Success)
            {
                this.NextIndex[request.Server] = this.Logs.Count + 1;
                this.MatchIndex[request.Server] = this.Logs.Count;

                this.VotesReceived++;
                if (request.ReceiverEndpoint != null &&
                    this.VotesReceived >= (this.Servers.Length / 2) + 1)
                {
                    this.Logger.WriteLine("\n [Leader] " + this.ServerId + " | term " + this.CurrentTerm +
                        " | append votes " + this.VotesReceived + " | append success\n");

                    var commitIndex = this.MatchIndex[request.Server];
                    if (commitIndex > this.CommitIndex &&
                        this.Logs[commitIndex - 1].Term == this.CurrentTerm)
                    {
                        this.CommitIndex = commitIndex;

                        this.Logger.WriteLine("\n [Leader] " + this.ServerId + " | term " + this.CurrentTerm +
                            " | log " + this.Logs.Count + " | command " + this.Logs[commitIndex - 1].Command + "\n");
                    }

                    this.VotesReceived = 0;
                    this.LastClientRequest = null;

                    this.Send(request.ReceiverEndpoint, new Client.Response());
                }
            }
            else
            {
                if (this.NextIndex[request.Server] > 1)
                {
                    this.NextIndex[request.Server] = this.NextIndex[request.Server] - 1;
                }

                var logs = this.Logs.GetRange(this.NextIndex[request.Server] - 1,
                    this.Logs.Count - (this.NextIndex[request.Server] - 1));

                var prevLogIndex = this.NextIndex[request.Server] - 1;
                var prevLogTerm = this.GetLogTermForIndex(prevLogIndex);

                this.Logger.WriteLine("\n [Leader] " + this.ServerId + " | term " + this.CurrentTerm + " | log " +
                    this.Logs.Count + " | append votes " + this.VotesReceived +
                    " | append fail (next idx = " + this.NextIndex[request.Server] + ")\n");

                this.Send(request.Server, new AppendEntriesRequest(this.CurrentTerm, this.Id, prevLogIndex,
                    prevLogTerm, logs, this.CommitIndex, request.ReceiverEndpoint));
            }
        }

        #endregion

        #region general methods

        /// <summary>
        /// Processes the given vote request.
        /// </summary>
        /// <param name="request">VoteRequest</param>
        void Vote(VoteRequest request)
        {
            var lastLogIndex = this.Logs.Count;
            var lastLogTerm = this.GetLogTermForIndex(lastLogIndex);

            if (request.Term < this.CurrentTerm ||
                (this.VotedFor != null && this.VotedFor != request.CandidateId) ||
                lastLogIndex > request.LastLogIndex ||
                lastLogTerm > request.LastLogTerm)
            {
                this.Logger.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm +
                    " | log " + this.Logs.Count + " | vote false\n");
                this.Send(request.CandidateId, new VoteResponse(this.CurrentTerm, false));
            }
            else
            {
                this.Logger.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm +
                    " | log " + this.Logs.Count + " | vote true\n");

                this.VotedFor = request.CandidateId;
                this.LeaderId = null;

                this.Send(request.CandidateId, new VoteResponse(this.CurrentTerm, true));
            }
        }

        /// <summary>
        /// Processes the given append entries request.
        /// </summary>
        /// <param name="request">AppendEntriesRequest</param>
        void AppendEntries(AppendEntriesRequest request)
        {
            if (request.Term < this.CurrentTerm)
            {
                this.Logger.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm + " | log " +
                    this.Logs.Count + " | last applied: " + this.LastApplied + " | append false (< term)\n");

                this.Send(request.LeaderId, new AppendEntriesResponse(this.CurrentTerm, false,
                    this.Id, request.ReceiverEndpoint));
            }
            else
            {
                if (request.PrevLogIndex > 0 &&
                    (this.Logs.Count < request.PrevLogIndex ||
                    this.Logs[request.PrevLogIndex - 1].Term != request.PrevLogTerm))
                {
                    this.Logger.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm + " | log " +
                        this.Logs.Count + " | last applied: " + this.LastApplied + " | append false (not in log)\n");

                    this.Send(request.LeaderId, new AppendEntriesResponse(this.CurrentTerm,
                        false, this.Id, request.ReceiverEndpoint));
                }
                else
                {
                    if (request.Entries.Count > 0)
                    {
                        var currentIndex = request.PrevLogIndex + 1;
                        foreach (var entry in request.Entries)
                        {
                            if (this.Logs.Count < currentIndex)
                            {
                                this.Logs.Add(entry);
                            }
                            else if (this.Logs[currentIndex - 1].Term != entry.Term)
                            {
                                this.Logs.RemoveRange(currentIndex - 1, this.Logs.Count - (currentIndex - 1));
                                this.Logs.Add(entry);
                            }

                            currentIndex++;
                        }
                    }

                    if (request.LeaderCommit > this.CommitIndex &&
                        this.Logs.Count < request.LeaderCommit)
                    {
                        this.CommitIndex = this.Logs.Count;
                    }
                    else if (request.LeaderCommit > this.CommitIndex)
                    {
                        this.CommitIndex = request.LeaderCommit;
                    }

                    if (this.CommitIndex > this.LastApplied)
                    {
                        this.LastApplied++;
                    }

                    this.Logger.WriteLine("\n [Server] " + this.ServerId + " | term " + this.CurrentTerm + " | log " +
                        this.Logs.Count + " | entries received " + request.Entries.Count + " | last applied " +
                        this.LastApplied + " | append true\n");

                    this.LeaderId = request.LeaderId;
                    this.Send(request.LeaderId, new AppendEntriesResponse(this.CurrentTerm,
                        true, this.Id, request.ReceiverEndpoint));
                }
            }
        }

        void RedirectLastClientRequestToClusterManager()
        {
            if (this.LastClientRequest != null)
            {
                this.Send(this.ClusterManager, this.LastClientRequest);
            }
        }

        /// <summary>
        /// Returns the log term for the given log index.
        /// </summary>
        /// <param name="logIndex">Index</param>
        /// <returns>Term</returns>
        int GetLogTermForIndex(int logIndex)
        {
            var logTerm = 0;
            if (logIndex > 0)
            {
                logTerm = this.Logs[logIndex - 1].Term;
            }

            return logTerm;
        }

        void ShuttingDown()
        {
            this.Send(this.ElectionTimer, new Halt());
            this.Send(this.PeriodicTimer, new Halt());

            this.Raise(new Halt());
        }

        #endregion
    }
}
