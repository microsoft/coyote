// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable SA1005 // Single line comments should begin with single space

//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Runtime.CompilerServices;
//using System.Threading.Tasks;
//using Microsoft.Coyote.Actors.Mocks;
//using Microsoft.Coyote.Actors.Timers;
//using Microsoft.Coyote.Actors.Timers.Mocks;
//using Microsoft.Coyote.Coverage;
//using Microsoft.Coyote.IO;
//using Microsoft.Coyote.Runtime;
//using Microsoft.Coyote.Specifications;
//using Microsoft.Coyote.SystematicTesting;
//using Monitor = Microsoft.Coyote.Specifications.Monitor;

//namespace Microsoft.Coyote.Actors
//{
//    /// <summary>
//    /// Runtime for creating, executing and controlling actors.
//    /// </summary>
//    internal sealed class ActorRuntime : IActorRuntime
//    {
//        /// <summary>
//        /// The configuration used by the runtime.
//        /// </summary>
//        internal readonly Configuration Configuration;

//        /// <summary>
//        /// The default actor manager.
//        /// </summary>
//        private readonly ActorManager DefaultActorManager;

//        /// <summary>
//        /// Responsible for checking specifications.
//        /// </summary>
//        private readonly SpecificationEngine SpecificationEngine;

//        /// <summary>
//        /// The asynchronous operation scheduler.
//        /// </summary>
//        internal readonly OperationScheduler Scheduler;

//        /// <summary>
//        /// Data structure containing information regarding testing coverage.
//        /// </summary>
//        internal CoverageInfo CoverageInfo;

//        /// <summary>
//        /// Map from controlled tasks to their corresponding operations,
//        /// if such an operation exists.
//        /// </summary>
//        private readonly ConcurrentDictionary<Task, TaskOperation> TaskMap;

//        /// <summary>
//        /// Map that stores all unique names and their corresponding actor ids.
//        /// </summary>
//        internal readonly ConcurrentDictionary<string, ActorId> NameValueToActorId;

//        /// <summary>
//        /// Responsible for generating random values.
//        /// </summary>
//        private readonly IRandomValueGenerator ValueGenerator;

//        /// <summary>
//        /// Responsible for writing to all registered <see cref="IActorRuntimeLog"/> objects.
//        /// </summary>
//        internal LogWriter LogWriter { get; private set; }

//        /// <summary>
//        /// Used to log text messages. Use <see cref="ICoyoteRuntime.SetLogger"/>
//        /// to replace the logger with a custom one.
//        /// </summary>
//        public ILogger Logger
//        {
//            get { return this.LogWriter.Logger; }
//            set { using var v = this.LogWriter.SetLogger(value); }
//        }

//        /// <summary>
//        /// The root task id.
//        /// </summary>
//        internal readonly int? RootTaskId;

//        /// <summary>
//        /// Callback that is fired when a Coyote event is dropped. This happens when
//        /// <see cref="IActorRuntime.SendEvent"/> is called with an ActorId that has no matching
//        /// actor defined or the actor is halted.
//        /// </summary>
//        public event OnEventDroppedHandler OnEventDropped;

//        /// <summary>
//        /// Callback that is fired when an exception is thrown that includes failed assertions.
//        /// </summary>
//        public event OnFailureHandler OnFailure;

//        /// <summary>
//        /// If true, the actor execution is controlled to explore interleavings, else false.
//        /// </summary>
//        internal bool IsControlled { get; private set; }

//        /// <summary>
//        /// Initializes a new instance of the <see cref="ActorRuntime"/> class.
//        /// </summary>
//        internal ActorRuntime(Configuration configuration, SpecificationEngine specificationEngine, OperationScheduler scheduler,
//            IRandomValueGenerator valueGenerator, LogWriter logWriter, bool isControlled)
//        {
//            this.Configuration = configuration;
//            this.DefaultActorManager = isControlled ?
//                new MockActorManager(configuration, scheduler, this, specificationEngine, null, null, logWriter) :
//                new ActorManager(configuration, scheduler, this, specificationEngine, null, null, logWriter);
//            this.SpecificationEngine = specificationEngine;
//            this.Scheduler = scheduler;
//            this.CoverageInfo = new CoverageInfo();
//            this.TaskMap = new ConcurrentDictionary<Task, TaskOperation>();
//            this.NameValueToActorId = new ConcurrentDictionary<string, ActorId>();
//            this.ValueGenerator = valueGenerator;
//            this.LogWriter = logWriter;
//            // this.LogWriter = new LogWriter(configuration);
//            this.RootTaskId = Task.CurrentId;
//            this.IsControlled = isControlled;
//        }

//        /// <inheritdoc/>
//        public ActorId CreateActorId(Type type, string name = null) => this.DefaultActorManager.CreateActorId(type, name);

//        /// <inheritdoc/>
//        public ActorId CreateActorIdFromName(Type type, string name) => this.DefaultActorManager.CreateActorIdFromName(type, name);

//        /// <inheritdoc/>
//        public ActorId CreateActor(Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
//            this.DefaultActorManager.CreateActor(type, initialEvent, group);

//        /// <inheritdoc/>
//        public ActorId CreateActor(Type type, string name, Event initialEvent = null, EventGroup eventGroup = null) =>
//            this.DefaultActorManager.CreateActor(type, name, initialEvent, group);

//        /// <inheritdoc/>
//        public ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
//            this.DefaultActorManager.CreateActor(id, type, initialEvent, group);

//        /// <inheritdoc/>
//        public Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
//            this.DefaultActorManager.CreateActorAndExecuteAsync(type, initialEvent, group);

//        /// <inheritdoc/>
//        public Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null, EventGroup eventGroup = null) =>
//            this.DefaultActorManager.CreateActorAndExecuteAsync(type, name, initialEvent, group);

//        /// <inheritdoc/>
//        public Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
//            this.DefaultActorManager.CreateActorAndExecuteAsync(id, type, initialEvent, group);

//        /// <inheritdoc/>
//        public void SendEvent(ActorId targetId, Event initialEvent, EventGroup eventGroup = default, SendOptions options = null) =>
//            this.DefaultActorManager.SendEvent(targetId, initialEvent, group, options);

//        /// <inheritdoc/>
//        public Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event initialEvent, EventGroup eventGroup = null, SendOptions options = null) =>
//            this.DefaultActorManager.SendEventAndExecuteAsync(targetId, initialEvent, group, options);

//        /// <inheritdoc/>
//        public EventGroup GetCurrentEventGroup(ActorId currentActorId) => this.DefaultActorManager.GetCurrentEventGroup(currentActorId);

//        /// <summary>
//        /// Registers a new specification monitor of the specified <see cref="Type"/>.
//        /// </summary>
//        public void RegisterMonitor<T>()
//            where T : Monitor =>
//            this.TryCreateMonitor(typeof(T));

//        /// <summary>
//        /// Invokes the specified monitor with the specified <see cref="Event"/>.
//        /// </summary>
//        public void Monitor<T>(Event e)
//            where T : Monitor
//        {
//            // If the event is null then report an error and exit.
//            this.Assert(e != null, "Cannot monitor a null event.");
//            this.InvokeMonitor(typeof(T), e, null, null, null);
//        }

//        /// <summary>
//        /// Tries to create a new <see cref="Specifications.Monitor"/> of the specified <see cref="Type"/>.
//        /// </summary>
//        internal bool TryCreateMonitor(Type type) => this.SpecificationEngine.TryCreateMonitor(type, this.CoverageInfo, this.LogWriter);

//        /// <summary>
//        /// Invokes the specified <see cref="Specifications.Monitor"/> with the specified <see cref="Event"/>.
//        /// </summary>
//        internal void InvokeMonitor(Type type, Event e, string senderName, string senderType, string senderStateName) =>
//            this.SpecificationEngine.InvokeMonitor(type, e, senderName, senderType, senderStateName);

//        /// <summary>
//        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
//        /// </summary>
//        public void Assert(bool predicate) => this.SpecificationEngine.Assert(predicate);

//        /// <summary>
//        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
//        /// </summary>
//        public void Assert(bool predicate, string s, object arg0) => this.SpecificationEngine.Assert(predicate, s, arg0);

//        /// <summary>
//        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
//        /// </summary>
//        public void Assert(bool predicate, string s, object arg0, object arg1) => this.SpecificationEngine.Assert(predicate, s, arg0, arg1);

//        /// <summary>
//        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
//        /// </summary>
//        public void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
//            this.SpecificationEngine.Assert(predicate, s, arg0, arg1, arg2);

//        /// <summary>
//        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
//        /// </summary>
//        public void Assert(bool predicate, string s, params object[] args) => this.SpecificationEngine.Assert(predicate, s, args);

//        /// <summary>
//        /// Returns a nondeterministic boolean choice, that can be controlled
//        /// during analysis or testing.
//        /// </summary>
//        public bool RandomBoolean() => this.GetNondeterministicBooleanChoice(2, null, null);

//        /// <summary>
//        /// Returns a nondeterministic boolean choice, that can be controlled
//        /// during analysis or testing. The value is used to generate a number
//        /// in the range [0..maxValue), where 0 triggers true.
//        /// </summary>
//        public bool RandomBoolean(int maxValue) => this.GetNondeterministicBooleanChoice(maxValue, null, null);

//        /// <summary>
//        /// Returns a controlled nondeterministic boolean choice.
//        /// </summary>
//        internal bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType)
//        {
//            if (this.IsControlled)
//            {
//                var caller = this.Scheduler.GetExecutingOperation<ActorOperation>()?.Actor;
//                if (caller is StateMachine callerStateMachine)
//                {
//                    (callerStateMachine.Manager as MockStateMachineManager).ProgramCounter++;
//                }
//                else if (caller is Actor callerActor)
//                {
//                    (callerActor.Manager as MockActorManager).ProgramCounter++;
//                }

//                var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(maxValue);
//                this.LogWriter.LogRandom(choice, callerName ?? caller?.Id.Name, callerType ?? caller?.Id.Type);
//                return choice;
//            }
//            else
//            {
//                bool result = false;
//                if (this.ValueGenerator.Next(maxValue) == 0)
//                {
//                    result = true;
//                }

//                this.LogWriter.LogRandom(result, callerName, callerType);
//                return result;
//            }
//        }

//        /// <summary>
//        /// Returns a nondeterministic integer, that can be controlled during
//        /// analysis or testing. The value is used to generate an integer in
//        /// the range [0..maxValue).
//        /// </summary>
//        public int RandomInteger(int maxValue) => this.GetNondeterministicIntegerChoice(maxValue, null, null);

//        /// <summary>
//        /// Returns a controlled nondeterministic integer choice.
//        /// </summary>
//        internal int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType)
//        {
//            if (this.IsControlled)
//            {
//                var caller = this.Scheduler.GetExecutingOperation<ActorOperation>()?.Actor;
//                if (caller is StateMachine callerStateMachine)
//                {
//                    (callerStateMachine.Manager as MockStateMachineManager).ProgramCounter++;
//                }
//                else if (caller is Actor callerActor)
//                {
//                    (callerActor.Manager as MockActorManager).ProgramCounter++;
//                }

//                var choice = this.Scheduler.GetNextNondeterministicIntegerChoice(maxValue);
//                this.LogWriter.LogRandom(choice, callerName ?? caller?.Id.Name, callerType ?? caller?.Id.Type);
//                return choice;
//            }
//            else
//            {
//                var result = this.ValueGenerator.Next(maxValue);
//                this.LogWriter.LogRandom(result, callerName, callerType);
//                return result;
//            }
//        }

//        /// <summary>
//        /// Get the coverage graph information (if any). This information is only available
//        /// when <see cref="Configuration.ReportActivityCoverage"/> is enabled.
//        /// </summary>
//        /// <returns>A new CoverageInfo object.</returns>
//        public CoverageInfo GetCoverageInfo()
//        {
//            var result = this.CoverageInfo;
//            if (result != null)
//            {
//                var builder = this.LogWriter.GetLogsOfType<ActorRuntimeLogGraphBuilder>().FirstOrDefault();
//                if (builder != null)
//                {
//                    result.CoverageGraph = builder.SnapshotGraph(this.Configuration.IsDgmlBugGraph);
//                }

//                var eventCoverage = this.LogWriter.GetLogsOfType<ActorRuntimeLogEventCoverage>().FirstOrDefault();
//                if (eventCoverage != null)
//                {
//                    result.EventInfo = eventCoverage.EventCoverage;
//                }
//            }

//            return result;
//        }


//        /// <inheritdoc/>
//        [Obsolete("Please set the Logger property directory instead of calling this method.")]
//        public TextWriter SetLogger(TextWriter logger)
//        {
//            var result = this.LogWriter.SetLogger(new TextWriterLogger(logger));
//            if (result != null)
//            {
//                return result.TextWriter;
//            }

//            return null;
//        }

//        /// <summary>
//        /// Use this method to register an <see cref="IActorRuntimeLog"/>.
//        /// </summary>
//        public void RegisterLog(IActorRuntimeLog log) => this.LogWriter.RegisterLog(log);

//        /// <summary>
//        /// Use this method to unregister a previously registered <see cref="IActorRuntimeLog"/>.
//        /// </summary>
//        public void RemoveLog(IActorRuntimeLog log) => this.LogWriter.RemoveLog(log);

//        /// <summary>
//        /// Terminates the runtime.
//        /// </summary>
//        public void Stop() => this.Scheduler.ForceStop();

//        /// <summary>
//        /// Disposes runtime resources.
//        /// </summary>
//        private void Dispose(bool disposing)
//        {
//            if (disposing)
//            {
//                this.ActorMap.Clear();
//            }
//        }

//        /// <summary>
//        /// Disposes runtime resources.
//        /// </summary>
//        public void Dispose()
//        {
//            this.Dispose(true);
//            GC.SuppressFinalize(this);
//        }
//    }
//}
