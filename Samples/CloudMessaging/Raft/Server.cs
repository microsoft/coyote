// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// A Raft server implemented using a <see cref="StateMachine"/>. The server
    /// transitions between being a follower, candidate or leader. Each of these
    /// roles is implemented as a <see cref="StateMachine.State"/>.
    /// </summary>
    public class Server : StateMachine
    {
        /// <summary>
        /// Event that configures a Raft server.
        /// </summary>
        public class SetupServerEvent : Event
        {
            public readonly IServerManager ServerManager;
            public readonly ActorId ClusterManager;

            public SetupServerEvent(IServerManager serverManager, ActorId clusterManager)
            {
                this.ServerManager = serverManager;
                this.ClusterManager = clusterManager;
            }
        }

        /// <summary>
        /// Manages this server instance.
        /// </summary>
        private IServerManager Manager;

        /// <summary>
        /// The id of the ClusterManager state machine.
        /// </summary>
        private ActorId ClusterManager;

        /// <summary>
        /// Latest term server has seen (initialized to 0 on
        /// first boot, increases monotonically).
        /// </summary>
        private int CurrentTerm;

        /// <summary>
        /// The candidate id that received vote in current term (or null if none).
        /// </summary>
        private string VotedFor;

        /// <summary>
        /// Index of highest log entry known to be committed (initialized
        /// to 0, increases monotonically).
        /// </summary>
        private int CommitIndex;

        /// <summary>
        /// Index of highest log entry applied to state machine (initialized
        /// to 0, increases monotonically).
        /// </summary>
        private int LastApplied;

        /// <summary>
        /// Log entries.
        /// </summary>
        private List<Log> Logs;

        /// <summary>
        /// For each server, index of the next log entry to send to that
        /// server (initialized to leader last log index + 1).
        /// </summary>
        private Dictionary<string, int> NextIndex;

        /// <summary>
        /// For each server, index of highest log entry known to be replicated
        /// on server (initialized to 0, increases monotonically).
        /// </summary>
        private Dictionary<string, int> MatchIndex;

        /// <summary>
        /// Number of canditate votes received.
        /// </summary>
        private int VotesReceived;

        /// <summary>
        /// Number of log entry votes received.
        /// </summary>
        private int LogVotesReceived;

        /// <summary>
        /// Previously handled client requests.
        /// </summary>
        private HashSet<string> HandledClientRequests;

        /// <summary>
        /// The leader election timer.
        /// </summary>
        private TimerInfo LeaderElectionTimer;

        [Start]
        [OnEventGotoState(typeof(NotifyJoinedServiceEvent), typeof(Follower))]
        [DeferEvents(typeof(WildCardEvent))]
        private class Init : State { }

        [OnEntry(nameof(BecomeFollower))]
        [OnEventDoAction(typeof(VoteRequestEvent), nameof(VoteRequest))]
        [OnEventDoAction(typeof(VoteResponseEvent), nameof(VoteResponse))]
        [OnEventDoAction(typeof(AppendLogEntriesRequestEvent), nameof(AppendLogEntriesRequest))]
        [OnEventDoAction(typeof(AppendLogEntriesResponseEvent), nameof(AppendLogEntriesResponse))]
        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
        [IgnoreEvents(typeof(ClientRequestEvent), typeof(ClientResponseEvent))]
        private class Follower : State { }

        [OnEntry(nameof(BecomeCandidate))]
        [OnEventDoAction(typeof(VoteRequestEvent), nameof(VoteRequest))]
        [OnEventDoAction(typeof(VoteResponseEvent), nameof(VoteResponse))]
        [OnEventDoAction(typeof(AppendLogEntriesRequestEvent), nameof(AppendLogEntriesRequest))]
        [OnEventDoAction(typeof(AppendLogEntriesResponseEvent), nameof(AppendLogEntriesResponse))]
        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
        [IgnoreEvents(typeof(ClientRequestEvent), typeof(ClientResponseEvent))]
        private class Candidate : State { }

        [OnEntry(nameof(BecomeLeader))]
        [OnEventDoAction(typeof(ClientRequestEvent), nameof(HandleClientRequest))]
        [OnEventDoAction(typeof(VoteRequestEvent), nameof(VoteRequest))]
        [OnEventDoAction(typeof(VoteResponseEvent), nameof(VoteResponse))]
        [OnEventDoAction(typeof(AppendLogEntriesRequestEvent), nameof(AppendLogEntriesRequest))]
        [OnEventDoAction(typeof(AppendLogEntriesResponseEvent), nameof(AppendLogEntriesResponse))]
        [IgnoreEvents(typeof(TimerElapsedEvent), typeof(ClientResponseEvent))]
        private class Leader : State { }

        /// <summary>
        /// Asynchronous callback that is invoked when the server is initialized.
        /// </summary>>
        protected override Task OnInitializeAsync(Event initialEvent)
        {
            var setupEvent = initialEvent as SetupServerEvent;
            this.Manager = setupEvent.ServerManager;
            this.ClusterManager = setupEvent.ClusterManager;

            this.CurrentTerm = 0;
            this.CommitIndex = 0;
            this.LastApplied = 0;
            this.VotedFor = string.Empty;

            this.Logs = new List<Log>();
            this.NextIndex = new Dictionary<string, int>();
            this.MatchIndex = new Dictionary<string, int>();
            this.HandledClientRequests = new HashSet<string>();

            return Task.CompletedTask;
        }

        private void StartTimer()
        {
            if (this.LeaderElectionTimer is null)
            {
                // Start a periodic leader election timer.
                this.LeaderElectionTimer = this.StartPeriodicTimer(this.Manager.LeaderElectionDueTime,
                    this.Manager.LeaderElectionPeriod);
            }
        }

        /// <summary>
        /// Asynchronous callback that initializes the server upon
        /// transition to a new role.
        /// </summary>
        private void BecomeFollower()
        {
            this.StartTimer();
            this.VotesReceived = 0;
        }

        private void BecomeCandidate()
        {
            this.StartTimer();

            this.CurrentTerm++;
            this.VotedFor = this.Manager.ServerId;
            this.VotesReceived = 1;

            var lastLogIndex = this.Logs.Count;
            var lastLogTerm = lastLogIndex > 0 ? this.Logs[lastLogIndex - 1].Term : 0;

            this.SendEvent(this.ClusterManager, new VoteRequestEvent(this.CurrentTerm, this.Manager.ServerId, lastLogIndex, lastLogTerm));
            this.Logger.WriteLine($"<VoteRequest> {this.Manager.ServerId} sent vote request " +
                $"(term={this.CurrentTerm}, lastLogIndex={lastLogIndex}, lastLogTerm={lastLogTerm}).");
        }

        private void BecomeLeader()
        {
            this.Manager.NotifyElectedLeader(this.CurrentTerm);

            var logIndex = this.Logs.Count;
            var logTerm = logIndex > 0 ? this.Logs[logIndex - 1].Term : 0;

            this.NextIndex.Clear();
            this.MatchIndex.Clear();
            foreach (var serverId in this.Manager.RemoteServerIds)
            {
                this.NextIndex.Add(serverId, logIndex + 1);
                this.MatchIndex.Add(serverId, 0);
            }

            foreach (var serverId in this.Manager.RemoteServerIds)
            {
                this.SendEvent(this.ClusterManager, new AppendLogEntriesRequestEvent(serverId, this.Manager.ServerId, this.CurrentTerm, logIndex,
                    logTerm, new List<Log>(), this.CommitIndex, string.Empty));
                this.Logger.WriteLine($"<AppendLogEntriesRequest> {this.Manager.ServerId} new leader sent append " +
                    $"entries request to {serverId} (term={this.CurrentTerm}, " +
                    $"prevLogIndex={logIndex}, prevLogTerm={logTerm}, " +
                    $"#entries=0, leaderCommit={this.CommitIndex})");
            }
        }

        /// <summary>
        /// Handle the received <see cref="VoteRequestEvent"/> by voting based
        /// on the current role of the Raft server.
        /// </summary>
        private void VoteRequest(Event e)
        {
            var request = e as VoteRequestEvent;
            this.Logger.WriteLine($"<VoteRequest> {this.Manager.ServerId} received vote request from " +
                $"{request.CandidateId} (term={request.Term}, lastLogIndex={request.LastLogIndex}, " +
                $"lastLogTerm={request.LastLogTerm}).");

            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = string.Empty;

                if (this.CurrentState == typeof(Candidate) || this.CurrentState == typeof(Leader))
                {
                    if (this.CurrentState == typeof(Leader))
                    {
                        this.Logger.WriteLine($"<Leader> {this.Manager.ServerId} is relinquishing leadership because VoteRequest term is {request.Term}");
                    }

                    this.RaiseGotoStateEvent<Follower>();
                }
            }

            var lastLogIndex = this.Logs.Count;
            var lastLogTerm = lastLogIndex > 0 ? this.Logs[lastLogIndex - 1].Term : 0;

            bool voteGranted = false;
            if ((this.VotedFor.Length == 0 || this.VotedFor == request.CandidateId) &&
                request.Term >= this.CurrentTerm && lastLogIndex <= request.LastLogIndex &&
                lastLogTerm <= request.LastLogTerm)
            {
                this.VotedFor = request.CandidateId;
                voteGranted = true;
            }

            this.SendEvent(this.ClusterManager, new VoteResponseEvent(request.CandidateId, this.CurrentTerm, voteGranted));
            this.Logger.WriteLine($"<VoteResponse> {this.Manager.ServerId} sent vote response " +
                $"(term={this.CurrentTerm}, log={this.Logs.Count}, vote={voteGranted}).");
        }

        /// <summary>
        /// Handle the received <see cref="VoteResponseEvent"/> based on the current role
        /// of the Raft server. If the server is in the <see cref="Candidate"/> role, and
        /// receives a vote majority, then it is elected as leader.
        /// </summary>
        private void VoteResponse(Event e)
        {
            var response = e as VoteResponseEvent;
            this.Logger.WriteLine($"<VoteResponse> {this.Manager.ServerId} received vote response " +
                $"(term={response.Term}, vote-granted={response.VoteGranted}).");

            if (response.Term > this.CurrentTerm)
            {
                this.CurrentTerm = response.Term;
                this.VotedFor = string.Empty;

                if (this.CurrentState == typeof(Candidate) || this.CurrentState == typeof(Leader))
                {
                    if (this.CurrentState == typeof(Leader))
                    {
                        this.Logger.WriteLine($"<Leader> {this.Manager.ServerId} is relinquishing leadership because VoteResponseEvent term is {response.Term}");
                    }

                    this.RaiseGotoStateEvent<Follower>();
                }
            }
            else if (this.CurrentState == typeof(Candidate) &&
                response.Term == this.CurrentTerm && response.VoteGranted)
            {
                this.VotesReceived++;
                if (this.VotesReceived >= (this.Manager.NumServers / 2) + 1)
                {
                    // A new leader is elected.
                    this.Logger.WriteLine($"<LeaderElection> {this.Manager.ServerId} was elected leader " +
                        $"(term={this.CurrentTerm}, #votes={this.VotesReceived}, log={this.Logs.Count}).");
                    this.VotesReceived = 0;
                    this.RaiseGotoStateEvent<Leader>();
                }
            }
        }

        /// <summary>
        /// Handle the received <see cref="AppendLogEntriesRequestEvent"/> based on
        /// the current role of the Raft server.
        /// </summary>
        private void AppendLogEntriesRequest(Event e)
        {
            var request = e as AppendLogEntriesRequestEvent;
            this.Logger.WriteLine($"<AppendLogEntriesRequest> {this.Manager.ServerId} received append " +
                $"entries request (term={request.Term}, leader={request.LeaderId}, " +
                $"prevLogIndex={request.PrevLogIndex}, prevLogTerm={request.PrevLogTerm}, " +
                $"#entries={request.Entries.Count}, leaderCommit={request.LeaderCommit})");

            bool appendEntries = this.CurrentState == typeof(Follower) ||
                this.CurrentState == typeof(Candidate);

            if (request.Term > this.CurrentTerm)
            {
                this.CurrentTerm = request.Term;
                this.VotedFor = string.Empty;

                if (this.CurrentState == typeof(Candidate))
                {
                    this.RaiseGotoStateEvent<Follower>();
                }
                else if (this.CurrentState == typeof(Leader))
                {
                    this.Logger.WriteLine($"<Leader> {this.Manager.ServerId} is relinquishing leadership because AppendLogEntriesRequestEvent term is {request.Term}");
                    appendEntries = true;
                    this.RaiseGotoStateEvent<Follower>();
                }
            }

            if (appendEntries)
            {
                if (request.Term < this.CurrentTerm)
                {
                    this.SendEvent(this.ClusterManager, new AppendLogEntriesResponseEvent(request.LeaderId, this.Manager.ServerId, this.CurrentTerm, false, request.Command));
                    this.Logger.WriteLine($"<AppendLogEntriesResponse> {this.Manager.ServerId} sent append " +
                        $"entries response (term={this.CurrentTerm}, log={this.Logs.Count}, " +
                        $"last-applied={this.LastApplied}, append=false[<term]) in state {this.CurrentState.Name}.");
                }
                else
                {
                    if (request.PrevLogIndex > 0 &&
                        (this.Logs.Count < request.PrevLogIndex ||
                        this.Logs[request.PrevLogIndex - 1].Term != request.PrevLogTerm))
                    {
                        this.SendEvent(this.ClusterManager, new AppendLogEntriesResponseEvent(request.LeaderId, this.Manager.ServerId, this.CurrentTerm, false, request.Command));
                        this.Logger.WriteLine($"<AppendLogEntriesResponse> {this.Manager.ServerId} sent append " +
                            $"entries response (term={this.CurrentTerm}, log={this.Logs.Count}, " +
                            $"last-applied={this.LastApplied}, append=false[missing]) in state {this.CurrentState.Name}.");
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

                        this.SendEvent(this.ClusterManager, new AppendLogEntriesResponseEvent(request.LeaderId, this.Manager.ServerId, this.CurrentTerm, true, request.Command));

                        this.Logger.WriteLine($"<AppendLogEntriesResponse> {this.Manager.ServerId} sent append " +
                            $"entries response (term={this.CurrentTerm}, log={this.Logs.Count}, " +
                            $"entries-received={request.Entries.Count}, last-applied={this.LastApplied}, " +
                            $"append=true) in state {this.CurrentState.Name}.");
                    }
                }
            }
        }

        /// <summary>
        /// Handle the received <see cref="AppendLogEntriesResponseEvent"/> based on
        /// the current role of the Raft server.
        /// </summary>
        private void AppendLogEntriesResponse(Event e)
        {
            var response = e as AppendLogEntriesResponseEvent;
            this.Logger.WriteLine($"<AppendLogEntriesResponse> {this.Manager.ServerId} received append entries " +
                $"response from {response.SenderId} (term={response.Term}, success={response.Success}) in state {this.CurrentState.Name}");

            if (response.Term > this.CurrentTerm)
            {
                this.CurrentTerm = response.Term;
                this.VotedFor = string.Empty;

                if (this.CurrentState == typeof(Candidate) || this.CurrentState == typeof(Leader))
                {
                    if (this.CurrentState == typeof(Leader))
                    {
                        this.Logger.WriteLine($"<Leader> {this.Manager.ServerId} is relinquishing leadership because AppendLogEntriesResponseEvent term is {response.Term}");
                    }

                    this.RaiseGotoStateEvent<Follower>();
                }
            }
            else if (this.CurrentState == typeof(Leader) && response.Term == this.CurrentTerm)
            {
                if (response.Success)
                {
                    this.NextIndex[response.SenderId] = this.Logs.Count + 1;
                    this.MatchIndex[response.SenderId] = this.Logs.Count;

                    this.LogVotesReceived++;
                    if (response.Command.Length > 0 &&
                        this.LogVotesReceived >= (this.Manager.NumServers / 2) + 1)
                    {
                        var commitIndex = this.MatchIndex[response.SenderId];
                        if (commitIndex > this.CommitIndex &&
                            this.Logs[commitIndex - 1].Term == this.CurrentTerm)
                        {
                            this.CommitIndex = commitIndex;
                        }

                        this.LogVotesReceived = 0;
                        this.HandledClientRequests.Add(response.Command);

                        this.SendEvent(this.ClusterManager, new ClientResponseEvent(response.Command, this.Manager.ServerId));
                        this.Logger.WriteLine($"<ClientResponse> {this.Manager.ServerId} sent " +
                            $"client response (command={response.Command})");
                    }
                    else
                    {
                        this.Logger.WriteLine($"<Leader> {this.Manager.ServerId} has {this.LogVotesReceived} of max possible {this.Manager.NumServers} on command {response.Command}");
                    }
                }
                else
                {
                    if (this.NextIndex[response.SenderId] > 1)
                    {
                        this.NextIndex[response.SenderId] = this.NextIndex[response.SenderId] - 1;
                    }

                    var entries = this.Logs.GetRange(this.NextIndex[response.SenderId] - 1, this.Logs.Count - (this.NextIndex[response.SenderId] - 1));
                    var prevLogIndex = this.NextIndex[response.SenderId] - 1;
                    var prevLogTerm = prevLogIndex > 0 ? this.Logs[prevLogIndex - 1].Term : 0;

                    this.SendEvent(this.ClusterManager, new AppendLogEntriesRequestEvent(response.SenderId, this.Manager.ServerId, this.CurrentTerm, prevLogIndex,
                        prevLogTerm, entries, this.CommitIndex, response.Command));

                    this.Logger.WriteLine($"<AppendLogEntriesRequest> {this.Manager.ServerId} sent append " +
                        $"entries request to {response.SenderId} (term={this.CurrentTerm}, " +
                        $"prevLogIndex={prevLogIndex}, prevLogTerm={prevLogTerm}, " +
                        $"#entries={entries.Count}, leaderCommit={this.CommitIndex})");
                }
            }
        }

        /// <summary>
        /// Handle the received <see cref="ClientRequestEvent"/>.
        /// </summary>
        private void HandleClientRequest(Event e)
        {
            var clientRequest = e as ClientRequestEvent;
            if (this.HandledClientRequests.Contains(clientRequest.Command))
            {
                return;
            }

            this.Logger.WriteLine($"<ClientRequest> {this.Manager.ServerId} received " +
                $"client request (command={clientRequest.Command})");

            // Append the command to the log.
            this.Logs.Add(new Log(this.CurrentTerm, clientRequest.Command));
            this.LogVotesReceived = 1;

            // Replicate the new log entries out to each remote server then wait for a majority of nodes to
            // respond with AppendLogEntriesResponseEvent before deciding if we can commit this new log entry
            // and then send the ClientResponseEvent.
            var lastLogIndex = this.Logs.Count;
            foreach (var serverId in this.Manager.RemoteServerIds)
            {
                if (lastLogIndex < this.NextIndex[serverId])
                {
                    continue;
                }

                var entries = this.Logs.GetRange(this.NextIndex[serverId] - 1,
                    this.Logs.Count - (this.NextIndex[serverId] - 1));
                var prevLogIndex = this.NextIndex[serverId] - 1;
                var prevLogTerm = prevLogIndex > 0 ? this.Logs[prevLogIndex - 1].Term : 0;
                this.SendEvent(this.ClusterManager, new AppendLogEntriesRequestEvent(serverId, this.Manager.ServerId, this.CurrentTerm, prevLogIndex,
                    prevLogTerm, entries, this.CommitIndex, clientRequest.Command));
                this.Logger.WriteLine($"<AppendLogEntriesRequest> {this.Manager.ServerId} leader sent append " +
                    $"entries request to {serverId} (term={this.CurrentTerm}, " +
                    $"prevLogIndex={prevLogIndex}, prevLogTerm={prevLogTerm}, " +
                    $"#entries={entries.Count}, leaderCommit={this.CommitIndex}, command={clientRequest.Command}");
            }
        }

        /// <summary>
        /// Handle the received <see cref="TimerElapsedEvent"/> to start a new
        /// leader election. This handler is only called when the server is in
        /// the <see cref="Follower"/> or <see cref="Candidate"/> role.
        /// </summary>
        private void HandleTimeout()
        {
            // The timeout happens too often during testing, so we add a Random 1 in 10 chance here
            // to reduce that a bit.
            if (this.Manager.TimeoutDelay == 0 || this.RandomBoolean(this.Manager.TimeoutDelay))
            {
                this.RaiseGotoStateEvent<Candidate>();
            }
        }

        protected override Task OnExceptionHandledAsync(Exception ex, Event e)
        {
            return base.OnExceptionHandledAsync(ex, e);
        }
    }
}
