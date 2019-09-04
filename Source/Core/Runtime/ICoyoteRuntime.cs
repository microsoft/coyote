// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Interface of the Coyote runtime.
    /// </summary>
    public interface ICoyoteRuntime : IDisposable
    {
        /// <summary>
        /// The installed logger.
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// Callback that is fired when the runtime throws an exception.
        /// </summary>
        event OnFailureHandler OnFailure;

        /// <summary>
        /// Callback that is fired when an event is dropped.
        /// </summary>
        event OnEventDroppedHandler OnEventDropped;

        /// <summary>
        /// Creates a fresh machine id that has not yet been bound to any machine.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="machineName">Optional machine name used for logging.</param>
        /// <returns>The result is the machine id.</returns>
        MachineId CreateMachineId(Type type, string machineName = null);

        /// <summary>
        /// Creates a machine id that is uniquely tied to the specified unique name. The
        /// returned machine id can either be a fresh id (not yet bound to any machine),
        /// or it can be bound to a previously created machine. In the second case, this
        /// machine id can be directly used to communicate with the corresponding machine.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="machineName">Unique name used to create or get the machine id.</param>
        /// <returns>The result is the machine id.</returns>
        MachineId CreateMachineIdFromName(Type type, string machineName);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The result is the machine id.</returns>
        MachineId CreateMachine(Type type, Event e = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="machineName">Optional machine name used for logging.</param>
        /// <param name="e">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The result is the machine id.</returns>
        MachineId CreateMachine(Type type, string machineName, Event e = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new machine of the specified type, using the specified <see cref="MachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The result is the machine id.</returns>
        MachineId CreateMachine(MachineId mid, Type type, Event e = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the machine id.</returns>
        Task<MachineId> CreateMachineAndExecuteAsync(Type type, Event e = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="machineName">Optional machine name used for logging.</param>
        /// <param name="e">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the machine id.</returns>
        Task<MachineId> CreateMachineAndExecuteAsync(Type type, string machineName, Event e = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the machine id.</returns>
        Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, Event e = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the machine id.</returns>
        [Obsolete("Please use ICoyoteRuntime.CreateMachineAndExecuteAsync(...) instead.")]
        Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="machineName">Optional machine name used for logging.</param>
        /// <param name="e">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the machine id.</returns>
        [Obsolete("Please use ICoyoteRuntime.CreateMachineAndExecuteAsync(...) instead.")]
        Task<MachineId> CreateMachineAndExecute(Type type, string machineName, Event e = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the machine id.</returns>
        [Obsolete("Please use ICoyoteRuntime.CreateMachineAndExecuteAsync(...) instead.")]
        Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, Event e = null, Guid opGroupId = default);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="target">The id of the target machine.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <param name="options">Optional configuration of a send operation.</param>
        void SendEvent(MachineId target, Event e, Guid opGroupId = default, SendOptions options = null);

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense.
        /// </summary>
        /// <param name="target">The id of the target machine.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <param name="options">Optional configuration of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is true if
        /// the event was handled, false if the event was only enqueued.</returns>
        Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, Guid opGroupId = default, SendOptions options = null);

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense.
        /// </summary>
        /// <param name="target">The id of the target machine.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <param name="options">Optional configuration of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is true if
        /// the event was handled, false if the event was only enqueued.</returns>
        [Obsolete("Please use ICoyoteRuntime.SendEventAndExecuteAsync(...) instead.")]
        Task<bool> SendEventAndExecute(MachineId target, Event e, Guid opGroupId = default, SendOptions options = null);

        /// <summary>
        /// Registers a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the monitor.</param>
        void RegisterMonitor(Type type);

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <typeparam name="T">Type of the monitor.</typeparam>
        /// <param name="e">Event</param>
        void InvokeMonitor<T>(Event e);

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <param name="type">Type of the monitor.</param>
        /// <param name="e">Event</param>
        void InvokeMonitor(Type type, Event e);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        /// <returns>The nondeterministic boolean choice.</returns>
        bool Random();

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="callerMemberName">CallerMemberName</param>
        /// <param name="callerFilePath">CallerFilePath</param>
        /// <param name="callerLineNumber">CallerLineNumber</param>
        /// <returns>The controlled nondeterministic choice.</returns>
        bool FairRandom(
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing. The value is used to generate a number
        /// in the range [0..maxValue), where 0 triggers true.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The nondeterministic boolean choice.</returns>
        bool Random(int maxValue);

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate an integer in the range [0..maxValue).
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The nondeterministic integer choice.</returns>
        int RandomInteger(int maxValue);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">The predicate to check.</param>
        void Assert(bool predicate);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">The predicate to check.</param>
        /// <param name="s">The message to print if the assertion fails.</param>
        /// <param name="arg0">The first argument.</param>
        void Assert(bool predicate, string s, object arg0);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">The predicate to check.</param>
        /// <param name="s">The message to print if the assertion fails.</param>
        /// <param name="arg0">The first argument.</param>
        /// <param name="arg1">The second argument.</param>
        void Assert(bool predicate, string s, object arg0, object arg1);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">The predicate to check.</param>
        /// <param name="s">The message to print if the assertion fails.</param>
        /// <param name="arg0">The first argument.</param>
        /// <param name="arg1">The second argument.</param>
        /// <param name="arg2">The third argument.</param>
        void Assert(bool predicate, string s, object arg0, object arg1, object arg2);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">The predicate to check.</param>
        /// <param name="s">The message to print if the assertion fails.</param>
        /// <param name="args">The message arguments.</param>
        void Assert(bool predicate, string s, params object[] args);

        /// <summary>
        /// Returns the operation group id of the specified machine id. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="MachineId"/> is not associated with this runtime.
        /// During testing, the runtime asserts that the specified machine is currently executing.
        /// </summary>
        /// <param name="currentMachineId">The id of the currently executing machine.</param>
        /// <returns>The unique identifier.</returns>
        Guid GetCurrentOperationGroupId(MachineId currentMachineId);

        /// <summary>
        /// Use this method to override the default <see cref="RuntimeLogWriter"/>
        /// for logging runtime messages.
        /// </summary>
        /// <param name="logWriter">The runtime log writer to install.</param>
        /// <returns>The previously installed runtime log writer.</returns>
        RuntimeLogWriter SetLogWriter(RuntimeLogWriter logWriter);

        /// <summary>
        /// Use this method to override the default <see cref="ILogger"/> for logging messages.
        /// </summary>
        /// <param name="logger">The logger to install.</param>
        /// <returns>The previously installed logger.</returns>
        ILogger SetLogger(ILogger logger);

        /// <summary>
        /// Terminates the runtime and notifies each active machine to halt execution.
        /// </summary>
        void Stop();
    }
}
