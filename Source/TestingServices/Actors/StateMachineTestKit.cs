// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.TestingServices.Runtime;

namespace Microsoft.Coyote.TestingServices
{
    /// <summary>
    /// Provides methods for testing a state machine of type <typeparamref name="T"/> in isolation.
    /// </summary>
    /// <typeparam name="T">The state machine type to test.</typeparam>
    public sealed class StateMachineTestKit<T>
        where T : StateMachine
    {
        /// <summary>
        /// The state machine testing runtime.
        /// </summary>
        private readonly ActorUnitTestingRuntime Runtime;

        /// <summary>
        /// The instance of the state machine being tested.
        /// </summary>
        public readonly T StateMachine;

        /// <summary>
        /// True if the state machine has started its execution, else false.
        /// </summary>
        private bool IsRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachineTestKit{T}"/> class.
        /// </summary>
        /// <param name="configuration">The runtime configuration to use.</param>
        public StateMachineTestKit(Configuration configuration)
        {
            configuration = configuration ?? Configuration.Create();
            this.Runtime = new ActorUnitTestingRuntime(typeof(T), configuration);
            this.StateMachine = this.Runtime.Instance as T;
            this.IsRunning = false;
            this.Runtime.OnFailure += ex =>
            {
                this.Runtime.Logger.WriteLine(ex.ToString());
            };
        }

        /// <summary>
        /// Transitions the state machine to its start state, passes the optional specified event and
        /// invokes its on-entry handler, if there is one available. This method returns a task that
        /// completes when the state machine reaches quiescence (typically when the event handler
        /// finishes executing because there are not more events to dequeue, or when the state
        /// machine asynchronously waits to receive an event).
        /// </summary>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public Task StartMachineAsync(Event initialEvent = null)
        {
            this.Runtime.Assert(!this.IsRunning,
                string.Format("'{0}' is already running.", this.StateMachine.Id));
            this.IsRunning = true;
            return this.Runtime.StartAsync(initialEvent);
        }

        /// <summary>
        /// Sends an event to the state machine and starts its event handler. This method returns
        /// a task that completes when the state machine reaches quiescence (typically when the
        /// event handler finishes executing because there are not more events to dequeue, or
        /// when the state machine asynchronously waits to receive an event).
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public Task SendEventAsync(Event e)
        {
            this.Runtime.Assert(this.IsRunning,
                string.Format("'{0}' is not running.", this.StateMachine.Id));
            return this.Runtime.SendEventAndExecuteAsync(this.Runtime.Instance.Id, e, null, Guid.Empty, null);
        }

        /// <summary>
        /// Invokes the state machine method with the specified name, and passing the specified
        /// optional parameters. Use this method to invoke private methods of the state machine.
        /// </summary>
        /// <param name="methodName">The name of the state machine method.</param>
        /// <param name="parameters">The parameters to the method.</param>
        public object Invoke(string methodName, params object[] parameters)
        {
            MethodInfo method = this.GetMethod(methodName, false, null);
            return method.Invoke(this.StateMachine, parameters);
        }

        /// <summary>
        /// Invokes the state machine method with the specified name and parameter types, and passing the
        /// specified optional parameters. Use this method to invoke private methods of the state machine.
        /// </summary>
        /// <param name="methodName">The name of the state machine method.</param>
        /// <param name="parameterTypes">The parameter types of the method.</param>
        /// <param name="parameters">The parameters to the method.</param>
        public object Invoke(string methodName, Type[] parameterTypes, params object[] parameters)
        {
            MethodInfo method = this.GetMethod(methodName, false, parameterTypes);
            return method.Invoke(this.StateMachine, parameters);
        }

        /// <summary>
        /// Invokes the asynchronous state machine method with the specified name, and passing the specified
        /// optional parameters. Use this method to invoke private methods of the state machine.
        /// </summary>
        /// <param name="methodName">The name of the state machine method.</param>
        /// <param name="parameters">The parameters to the method.</param>
        public async Task<object> InvokeAsync(string methodName, params object[] parameters)
        {
            MethodInfo method = this.GetMethod(methodName, true, null);
            var task = (Task)method.Invoke(this.StateMachine, parameters);
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty.GetValue(task);
        }

        /// <summary>
        /// Invokes the asynchronous state machine method with the specified name and parameter types, and passing
        /// the specified optional parameters. Use this method to invoke private methods of the state machine.
        /// </summary>
        /// <param name="methodName">The name of the state machine method.</param>
        /// <param name="parameterTypes">The parameter types of the method.</param>
        /// <param name="parameters">The parameters to the method.</param>
        public async Task<object> InvokeAsync(string methodName, Type[] parameterTypes, params object[] parameters)
        {
            MethodInfo method = this.GetMethod(methodName, true, parameterTypes);
            var task = (Task)method.Invoke(this.StateMachine, parameters);
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty.GetValue(task);
        }

        /// <summary>
        /// Uses reflection to get the state machine method with the specified name and parameter types.
        /// </summary>
        /// <param name="methodName">The name of the state machine method.</param>
        /// <param name="isAsync">True if the method is async, else false.</param>
        /// <param name="parameterTypes">The parameter types of the method.</param>
        private MethodInfo GetMethod(string methodName, bool isAsync, Type[] parameterTypes)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            MethodInfo method;
            if (parameterTypes is null)
            {
                method = this.StateMachine.GetType().GetMethod(methodName, bindingFlags);
            }
            else
            {
                method = this.StateMachine.GetType().GetMethod(methodName, bindingFlags,
                    Type.DefaultBinder, parameterTypes, null);
            }

            this.Runtime.Assert(method != null,
                string.Format("Unable to invoke method '{0}' in '{1}'.",
                methodName, this.StateMachine.Id));
            this.Runtime.Assert(method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) is null != isAsync,
                string.Format("Must invoke {0}method '{1}' of '{2}' using '{3}'.",
                isAsync ? string.Empty : "async ", methodName, this.StateMachine.Id, isAsync ? "Invoke" : "InvokeAsync"));

            return method;
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        public void Assert(bool predicate)
        {
            this.Runtime.Assert(predicate);
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        public void Assert(bool predicate, string s, object arg0)
        {
            this.Runtime.Assert(predicate, s, arg0);
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        public void Assert(bool predicate, string s, object arg0, object arg1)
        {
            this.Runtime.Assert(predicate, s, arg0, arg1);
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        public void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            this.Runtime.Assert(predicate, s, arg0, arg1, arg2);
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        public void Assert(bool predicate, string s, params object[] args)
        {
            this.Runtime.Assert(predicate, s, args);
        }

        /// <summary>
        /// Asserts that the state machine has transitioned to the state with the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="S">The type of the state.</typeparam>
        public void AssertStateTransition<S>()
            where S : StateMachine.State
        {
            this.AssertStateTransition(typeof(S).FullName);
        }

        /// <summary>
        /// Asserts that the state machine has transitioned to the state with the specified name
        /// (either <see cref="Type.FullName"/> or <see cref="MemberInfo.Name"/>).
        /// </summary>
        /// <param name="stateName">The name of the state.</param>
        public void AssertStateTransition(string stateName)
        {
            bool predicate = this.StateMachine.CurrentState.FullName.Equals(stateName) ||
                this.StateMachine.CurrentState.FullName.Equals(
                    this.StateMachine.CurrentState.DeclaringType.FullName + "+" + stateName);
            this.Runtime.Assert(predicate, string.Format("'{0}' is in state '{1}', not in '{2}'.",
                this.StateMachine.Id, this.StateMachine.CurrentState.FullName, stateName));
        }

        /// <summary>
        /// Asserts that the state machine is waiting (or not) to receive an event.
        /// </summary>
        public void AssertIsWaitingToReceiveEvent(bool isWaiting)
        {
            this.Runtime.Assert(this.Runtime.IsMachineWaitingToReceiveEvent == isWaiting,
                "'{0}' is {1}waiting to receive an event.",
                this.StateMachine.Id, this.Runtime.IsMachineWaitingToReceiveEvent ? string.Empty : "not ");
        }

        /// <summary>
        /// Asserts that the state machine inbox contains the specified number of events.
        /// </summary>
        /// <param name="numEvents">The number of events in the inbox.</param>
        public void AssertInboxSize(int numEvents)
        {
            this.Runtime.Assert(this.Runtime.ActorInbox.Size == numEvents,
                "'{0}' contains '{1}' events in its inbox.",
                this.StateMachine.Id, this.Runtime.ActorInbox.Size);
        }
    }
}
