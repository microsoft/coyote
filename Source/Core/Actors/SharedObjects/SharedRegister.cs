// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Actors.SharedObjects
{
    /// <summary>
    /// A thread-safe register that can be shared in-memory by actors.
    /// </summary>
    /// <remarks>
    /// See also <see href="/coyote/learn/programming-models/actors/sharing-objects">Sharing Objects</see>.
    /// </remarks>
    public static class SharedRegister
    {
        /// <summary>
        /// Creates a new shared register.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="runtime">The actor runtime.</param>
        /// <param name="value">The initial value.</param>
        public static SharedRegister<T> Create<T>(IActorRuntime runtime, T value = default)
            where T : struct
        {
            if (runtime is ActorExecutionContext.Mock executionContext)
            {
                return new Mock<T>(executionContext, value);
            }

            return new SharedRegister<T>(value);
        }

        /// <summary>
        /// Mock implementation of <see cref="SharedRegister{T}"/> that can be controlled during systematic testing.
        /// </summary>
        private sealed class Mock<T> : SharedRegister<T>
            where T : struct
        {
            // TODO: port to the new resource API or controlled locks once we integrate actors with tasks.

            /// <summary>
            /// Actor modeling the shared register.
            /// </summary>
            private readonly ActorId RegisterActor;

            /// <summary>
            /// The execution context associated with this shared register.
            /// </summary>
            private readonly ActorExecutionContext.Mock Context;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock{T}"/> class.
            /// </summary>
            internal Mock(ActorExecutionContext.Mock context, T value)
                : base(value)
            {
                this.Context = context;
                this.RegisterActor = context.CreateActor(typeof(SharedRegisterActor<T>));
                context.SendEvent(this.RegisterActor, SharedRegisterEvent.SetEvent(value));
            }

            /// <summary>
            /// Reads and updates the register.
            /// </summary>
            public override T Update(Func<T, T> func)
            {
                var op = this.Context.Scheduler.GetExecutingOperation<ActorOperation>();
                this.Context.SendEvent(this.RegisterActor, SharedRegisterEvent.UpdateEvent(func, op.Actor.Id));
                var e = op.Actor.ReceiveEventAsync(typeof(SharedRegisterResponseEvent<T>)).Result as SharedRegisterResponseEvent<T>;
                return e.Value;
            }

            /// <summary>
            /// Gets current value of the register.
            /// </summary>
            public override T GetValue()
            {
                var op = this.Context.Scheduler.GetExecutingOperation<ActorOperation>();
                this.Context.SendEvent(this.RegisterActor, SharedRegisterEvent.GetEvent(op.Actor.Id));
                var e = op.Actor.ReceiveEventAsync(typeof(SharedRegisterResponseEvent<T>)).Result as SharedRegisterResponseEvent<T>;
                return e.Value;
            }

            /// <summary>
            /// Sets current value of the register.
            /// </summary>
            public override void SetValue(T value)
            {
                this.Context.SendEvent(this.RegisterActor, SharedRegisterEvent.SetEvent(value));
            }
        }
    }

    /// <summary>
    /// A thread-safe register that can be shared in-memory by actors.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public class SharedRegister<T>
        where T : struct
    {
        /// <summary>
        /// Current value of the register.
        /// </summary>
        private protected T Value;

        /// <summary>
        /// Object used for synchronizing accesses to the register.
        /// </summary>
        private readonly object SynchronizationObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedRegister{T}"/> class.
        /// </summary>
        internal SharedRegister(T value)
        {
            this.Value = value;
            this.SynchronizationObject = new object();
        }

        /// <summary>
        /// Reads and updates the register.
        /// </summary>
        public virtual T Update(Func<T, T> func)
        {
            T oldValue, newValue;
            bool done = false;

            do
            {
                oldValue = this.Value;
                newValue = func(oldValue);

                lock (this.SynchronizationObject)
                {
                    if (oldValue.Equals(this.Value))
                    {
                        this.Value = newValue;
                        done = true;
                    }
                }
            }
            while (!done);

            return newValue;
        }

        /// <summary>
        /// Gets current value of the register.
        /// </summary>
        public virtual T GetValue()
        {
            T currentValue;
            lock (this.SynchronizationObject)
            {
                currentValue = this.Value;
            }

            return currentValue;
        }

        /// <summary>
        /// Sets current value of the register.
        /// </summary>
        public virtual void SetValue(T value)
        {
            lock (this.SynchronizationObject)
            {
                this.Value = value;
            }
        }
    }
}
