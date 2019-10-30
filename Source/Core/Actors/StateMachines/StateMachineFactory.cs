// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Factory for creating state machines.
    /// </summary>
    internal static class StateMachineFactory
    {
        /// <summary>
        /// Cache storing machine constructors.
        /// </summary>
        private static readonly Dictionary<Type, Func<StateMachine>> StateMachineConstructorCache =
            new Dictionary<Type, Func<StateMachine>>();

        /// <summary>
        /// Creates a new <see cref="StateMachine"/> of the specified type.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <returns>The created machine.</returns>
        public static StateMachine Create(Type type)
        {
            lock (StateMachineConstructorCache)
            {
                if (!StateMachineConstructorCache.TryGetValue(type, out Func<StateMachine> constructor))
                {
                    constructor = Expression.Lambda<Func<StateMachine>>(
                        Expression.New(type.GetConstructor(Type.EmptyTypes))).Compile();
                    StateMachineConstructorCache.Add(type, constructor);
                }

                return constructor();
            }
        }
    }
}
