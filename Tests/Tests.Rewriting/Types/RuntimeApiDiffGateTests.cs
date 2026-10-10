// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

using CoyoteInterlocked = Microsoft.Coyote.Rewriting.Types.Threading.Interlocked;
using CoyoteMonitor = Microsoft.Coyote.Rewriting.Types.Threading.Monitor;
using CoyoteSemaphoreSlim = Microsoft.Coyote.Rewriting.Types.Threading.SemaphoreSlim;
using CoyoteTask = Microsoft.Coyote.Rewriting.Types.Threading.Tasks.Task;
using CoyoteThread = Microsoft.Coyote.Rewriting.Types.Threading.Thread;
using CoyoteVolatile = Microsoft.Coyote.Rewriting.Types.Threading.Volatile;
using CoyoteWaitHandle = Microsoft.Coyote.Rewriting.Types.Threading.WaitHandle;
#if NET10_0_OR_GREATER
using CoyoteLock = Microsoft.Coyote.Rewriting.Types.Threading.Lock;
#endif

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class RuntimeApiDiffGateTests : BaseRewritingTest
    {
        private const string RunSynchronouslyReason =
            "Task.RunSynchronously is intentionally not controlled because it can execute work on an arbitrary scheduler.";

#if NET10_0_OR_GREATER
        private const string MemoryBarrierReason =
            "A memory barrier neither accesses shared memory nor blocks, so it is invoked natively without a " +
            "scheduling point, like Thread.MemoryBarrier; systematic testing executes one operation at a time " +
            "and does not explore the memory reorderings that the barrier prevents.";
#endif

        public RuntimeApiDiffGateTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestCurrentRuntimeSignaturesAreExactlyClassified()
        {
            IReadOnlyList<ApiMember> runtimeMembers = GetRuntimeMembers();
            IReadOnlyCollection<string> runtimeSignatures = runtimeMembers.Select(member => member.Signature).ToArray();
            IReadOnlyCollection<string> supportedSignatures = runtimeMembers
                .Where(member => member.IsSupported)
                .Select(member => member.Signature)
                .ToArray();
            IReadOnlyCollection<string> replacementSignatures = runtimeMembers
                .Where(member => member.IsSupported && HasReplacement(member))
                .Select(member => member.Signature)
                .ToArray();

            IReadOnlyList<string> errors = ApiDiffGate.Classify(
                runtimeSignatures,
                supportedSignatures,
                replacementSignatures,
                GetUnsupportedSignatures(runtimeMembers));
            Assert.True(errors.Count is 0, string.Join(Environment.NewLine, errors));
        }

        [Fact(Timeout = 5000)]
        public void TestAddedRuntimeMethodFailsTheGate()
        {
            IReadOnlyList<ApiMember> runtimeMembers = GetRuntimeMembers();
            IReadOnlyCollection<string> runtimeSignatures = runtimeMembers.Select(member => member.Signature)
                .Concat(new[] { "System.Threading.Tasks.Task|instance|AddedByANewRuntime()|System.Void" })
                .ToArray();
            IReadOnlyCollection<string> supportedSignatures = runtimeMembers
                .Where(member => member.IsSupported)
                .Select(member => member.Signature)
                .ToArray();
            IReadOnlyCollection<string> replacementSignatures = runtimeMembers
                .Where(member => member.IsSupported && HasReplacement(member))
                .Select(member => member.Signature)
                .ToArray();

            string error = Assert.Single(ApiDiffGate.Classify(
                runtimeSignatures,
                supportedSignatures,
                replacementSignatures,
                GetUnsupportedSignatures(runtimeMembers)));
            Assert.Contains("Unclassified runtime signature", error);
            Assert.Contains("AddedByANewRuntime", error);
        }

        [Fact(Timeout = 5000)]
        public void TestMissingSupportedReplacementMethodFailsTheGate()
        {
            IReadOnlyList<ApiMember> runtimeMembers = GetRuntimeMembers();
            ApiMember requiredMember = runtimeMembers.First(member => member.IsSupported);
            IReadOnlyCollection<string> runtimeSignatures = runtimeMembers.Select(member => member.Signature).ToArray();
            IReadOnlyCollection<string> supportedSignatures = runtimeMembers
                .Where(member => member.IsSupported)
                .Select(member => member.Signature)
                .ToArray();
            IReadOnlyCollection<string> replacementSignatures = runtimeMembers
                .Where(member => member.IsSupported && member.Signature != requiredMember.Signature && HasReplacement(member))
                .Select(member => member.Signature)
                .ToArray();

            string error = Assert.Single(ApiDiffGate.Classify(
                runtimeSignatures,
                supportedSignatures,
                replacementSignatures,
                GetUnsupportedSignatures(runtimeMembers)));
            Assert.Contains("Missing controlled replacement", error);
            Assert.Contains(requiredMember.Signature, error);
        }

        [Fact(Timeout = 5000)]
        public void TestAllowlistedMethodsRequireANonemptyReason()
        {
            IReadOnlyList<ApiMember> runtimeMembers = GetRuntimeMembers();
            ApiMember unsupportedMember = runtimeMembers.First(member => !member.IsSupported);
            IReadOnlyCollection<string> runtimeSignatures = runtimeMembers.Select(member => member.Signature).ToArray();
            IReadOnlyCollection<string> supportedSignatures = runtimeMembers
                .Where(member => member.IsSupported)
                .Select(member => member.Signature)
                .ToArray();
            IReadOnlyCollection<string> replacementSignatures = runtimeMembers
                .Where(member => member.IsSupported && HasReplacement(member))
                .Select(member => member.Signature)
                .ToArray();
            var unsupported = new Dictionary<string, string>(GetUnsupportedSignatures(runtimeMembers))
            {
                [unsupportedMember.Signature] = string.Empty
            };

            string error = Assert.Single(ApiDiffGate.Classify(
                runtimeSignatures,
                supportedSignatures,
                replacementSignatures,
                unsupported));
            Assert.Contains("nonempty reason", error);
            Assert.Contains(unsupportedMember.Signature, error);
        }

        [Fact(Timeout = 5000)]
        public void TestReplacementWithStricterGenericConstraintFailsTheGate()
        {
            ApiMember exchange = GetRuntimeMembers().Single(member =>
                member.RuntimeMethod.Name == nameof(Interlocked.Exchange) && member.RuntimeMethod.IsGenericMethodDefinition);
            Assert.True(HasReplacement(exchange));
            Assert.Contains("Exchange``1(!!0&,!!0)|!!0", exchange.Signature);

            var stricter = new ApiMember(exchange.RuntimeMethod, typeof(StricterInterlocked), null);
            Assert.False(HasReplacement(stricter));
            var nongeneric = new ApiMember(exchange.RuntimeMethod, typeof(NongenericInterlocked), null);
            Assert.False(HasReplacement(nongeneric));
        }

#if NET10_0_OR_GREATER
        [Fact(Timeout = 5000)]
        public void TestMemoryBarriersAreClassifiedAsPassThrough()
        {
            ApiMember[] barriers = GetRuntimeMembers()
                .Where(member => member.RuntimeMethod.DeclaringType == typeof(Volatile) &&
                    member.RuntimeMethod.Name is nameof(Volatile.ReadBarrier) or nameof(Volatile.WriteBarrier))
                .ToArray();
            Assert.Equal(2, barriers.Length);
            Assert.All(barriers, barrier =>
            {
                Assert.False(barrier.IsSupported);
                Assert.False(HasReplacement(barrier));
                Assert.False(string.IsNullOrWhiteSpace(barrier.PassThroughReason));
            });
        }
#endif

        private static IReadOnlyList<ApiMember> GetRuntimeMembers()
        {
            var members = new List<ApiMember>
            {
#if NET6_0_OR_GREATER
                Create(typeof(Task), typeof(CoyoteTask), nameof(Task.WaitAsync), typeof(CancellationToken)),
                Create(typeof(Task), typeof(CoyoteTask), nameof(Task.WaitAsync), typeof(TimeSpan)),
                Create(typeof(Task), typeof(CoyoteTask), nameof(Task.WaitAsync), typeof(TimeSpan), typeof(CancellationToken)),
#endif
#if NET8_0_OR_GREATER
                Create(typeof(Task), typeof(CoyoteTask), nameof(Task.WaitAsync), typeof(TimeSpan), typeof(TimeProvider)),
                Create(typeof(Task), typeof(CoyoteTask), nameof(Task.WaitAsync), typeof(TimeSpan), typeof(TimeProvider),
                    typeof(CancellationToken)),
#endif
#if NET6_0_OR_GREATER
                Create(typeof(Task<>), typeof(Microsoft.Coyote.Rewriting.Types.Threading.Tasks.Task<>),
                    nameof(Task.WaitAsync), typeof(CancellationToken)),
                Create(typeof(Task<>), typeof(Microsoft.Coyote.Rewriting.Types.Threading.Tasks.Task<>),
                    nameof(Task.WaitAsync), typeof(TimeSpan)),
                Create(typeof(Task<>), typeof(Microsoft.Coyote.Rewriting.Types.Threading.Tasks.Task<>),
                    nameof(Task.WaitAsync), typeof(TimeSpan), typeof(CancellationToken)),
#endif
#if NET8_0_OR_GREATER
                Create(typeof(Task<>), typeof(Microsoft.Coyote.Rewriting.Types.Threading.Tasks.Task<>),
                    nameof(Task.WaitAsync), typeof(TimeSpan), typeof(TimeProvider)),
                Create(typeof(Task<>), typeof(Microsoft.Coyote.Rewriting.Types.Threading.Tasks.Task<>),
                    nameof(Task.WaitAsync), typeof(TimeSpan), typeof(TimeProvider), typeof(CancellationToken)),
                Create(typeof(Task), typeof(CoyoteTask), nameof(Task.Delay), typeof(TimeSpan), typeof(TimeProvider)),
                Create(typeof(Task), typeof(CoyoteTask), nameof(Task.Delay), typeof(TimeSpan), typeof(TimeProvider),
                    typeof(CancellationToken)),
#endif
                Create(typeof(Monitor), typeof(CoyoteMonitor), nameof(Monitor.Enter), typeof(object)),
                Create(typeof(Monitor), typeof(CoyoteMonitor), nameof(Monitor.Exit), typeof(object)),
                Create(typeof(Monitor), typeof(CoyoteMonitor), nameof(Monitor.TryEnter), typeof(object)),
                Create(typeof(Monitor), typeof(CoyoteMonitor), nameof(Monitor.Wait), typeof(object)),
                Create(typeof(SemaphoreSlim), typeof(CoyoteSemaphoreSlim), nameof(SemaphoreSlim.Wait)),
                Create(typeof(SemaphoreSlim), typeof(CoyoteSemaphoreSlim), nameof(SemaphoreSlim.WaitAsync)),
                Create(typeof(SemaphoreSlim), typeof(CoyoteSemaphoreSlim), nameof(SemaphoreSlim.Release)),
                Create(typeof(Interlocked), typeof(CoyoteInterlocked), nameof(Interlocked.Increment),
                    typeof(int).MakeByRefType()),
                CreateGeneric(typeof(Interlocked), typeof(CoyoteInterlocked), nameof(Interlocked.Exchange), 2),
                CreateGeneric(typeof(Interlocked), typeof(CoyoteInterlocked), nameof(Interlocked.CompareExchange), 3),
                Create(typeof(WaitHandle), typeof(CoyoteWaitHandle), nameof(WaitHandle.WaitOne)),
                Create(typeof(Thread), typeof(CoyoteThread), nameof(Thread.Sleep), typeof(int)),
                CreatePassThrough(typeof(Task), typeof(CoyoteTask), nameof(Task.RunSynchronously), RunSynchronouslyReason)
            };

#if NET9_0_OR_GREATER
            foreach (Type type in new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort) })
            {
                members.Add(Create(typeof(Interlocked), typeof(CoyoteInterlocked), nameof(Interlocked.Exchange),
                    type.MakeByRefType(), type));
                members.Add(Create(typeof(Interlocked), typeof(CoyoteInterlocked), nameof(Interlocked.CompareExchange),
                    type.MakeByRefType(), type, type));
            }
#endif

#if NET10_0_OR_GREATER
            members.Add(Create(typeof(Task), typeof(CoyoteTask), nameof(Task.WaitAll),
                typeof(IEnumerable<Task>), typeof(CancellationToken)));
            members.Add(Create(typeof(Lock), typeof(CoyoteLock), nameof(Lock.Enter)));
            members.Add(Create(typeof(Lock), typeof(CoyoteLock), nameof(Lock.EnterScope)));
            members.Add(Create(typeof(Lock), typeof(CoyoteLock), nameof(Lock.TryEnter)));
            members.Add(Create(typeof(Lock), typeof(CoyoteLock), nameof(Lock.TryEnter), typeof(int)));
            members.Add(Create(typeof(Lock), typeof(CoyoteLock), nameof(Lock.TryEnter), typeof(TimeSpan)));
            members.Add(Create(typeof(Lock), typeof(CoyoteLock), nameof(Lock.Exit)));
            members.Add(Create(typeof(Lock), typeof(CoyoteLock), "get_IsHeldByCurrentThread"));
            members.Add(CreatePassThrough(typeof(Volatile), typeof(CoyoteVolatile), nameof(Volatile.ReadBarrier),
                MemoryBarrierReason));
            members.Add(CreatePassThrough(typeof(Volatile), typeof(CoyoteVolatile), nameof(Volatile.WriteBarrier),
                MemoryBarrierReason));
#endif
            return members;
        }

        private static ApiMember Create(Type runtimeType, Type replacementType, string methodName, params Type[] parameterTypes) =>
            new ApiMember(FindMethod(runtimeType, methodName, parameterTypes), replacementType, null);

        /// <summary>
        /// Creates a member that is deliberately left as a call to the runtime, with the reason why.
        /// </summary>
        private static ApiMember CreatePassThrough(Type runtimeType, Type replacementType, string methodName,
            string reason, params Type[] parameterTypes) =>
            new ApiMember(FindMethod(runtimeType, methodName, parameterTypes), replacementType, reason);

        private static MethodInfo FindMethod(Type runtimeType, string methodName, Type[] parameterTypes) =>
            runtimeType.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Single(candidate => candidate.Name == methodName && ParametersMatch(candidate, parameterTypes));

        private static ApiMember CreateGeneric(Type runtimeType, Type replacementType, string methodName,
            int parameterCount)
        {
            MethodInfo method = runtimeType.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Single(candidate => candidate.Name == methodName && candidate.IsGenericMethodDefinition &&
                    candidate.GetParameters().Length == parameterCount);
            return new ApiMember(method, replacementType, null);
        }

        private static bool ParametersMatch(MethodInfo method, IReadOnlyList<Type> parameterTypes)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != parameterTypes.Count)
            {
                return false;
            }

            for (int idx = 0; idx < parameters.Length; idx++)
            {
                if (parameters[idx].ParameterType != parameterTypes[idx])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasReplacement(ApiMember member)
        {
            MethodInfo runtimeMethod = member.RuntimeMethod;
            foreach (MethodInfo replacementMethod in member.ReplacementType.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (replacementMethod.Name != runtimeMethod.Name)
                {
                    continue;
                }

                ParameterInfo[] replacementParameters = replacementMethod.GetParameters();
                ParameterInfo[] runtimeParameters = runtimeMethod.GetParameters();
                int offset = runtimeMethod.IsStatic ? 0 : 1;
                if (replacementParameters.Length != runtimeParameters.Length + offset ||
                    !GenericParametersMatch(replacementMethod, runtimeMethod) ||
                    !TypeShapesMatch(replacementMethod.ReturnType, runtimeMethod.ReturnType))
                {
                    continue;
                }

                if (!runtimeMethod.IsStatic &&
                    !TypeShapesMatch(replacementParameters[0].ParameterType, runtimeMethod.DeclaringType))
                {
                    continue;
                }

                bool matched = true;
                for (int idx = 0; idx < runtimeParameters.Length; idx++)
                {
                    if (!TypeShapesMatch(replacementParameters[idx + offset].ParameterType,
                        runtimeParameters[idx].ParameterType))
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks that the replacement has the same generic arity as the runtime method, and that
        /// every generic argument that is valid for the runtime method is valid for the replacement,
        /// so that rewriting a runtime-valid generic call cannot violate a stricter constraint.
        /// </summary>
        private static bool GenericParametersMatch(MethodInfo replacementMethod, MethodInfo runtimeMethod)
        {
            Type[] replacementParameters = replacementMethod.IsGenericMethodDefinition ?
                replacementMethod.GetGenericArguments() : Type.EmptyTypes;
            Type[] runtimeParameters = runtimeMethod.IsGenericMethodDefinition ?
                runtimeMethod.GetGenericArguments() : Type.EmptyTypes;
            if (replacementParameters.Length != runtimeParameters.Length)
            {
                return false;
            }

            for (int idx = 0; idx < runtimeParameters.Length; idx++)
            {
                if (!IsConstraintNoStricter(replacementParameters[idx], runtimeParameters[idx]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsConstraintNoStricter(Type replacementParameter, Type runtimeParameter)
        {
            const GenericParameterAttributes ConstraintMask =
                GenericParameterAttributes.ReferenceTypeConstraint |
                GenericParameterAttributes.NotNullableValueTypeConstraint |
                GenericParameterAttributes.DefaultConstructorConstraint;
            GenericParameterAttributes replacementConstraints =
                replacementParameter.GenericParameterAttributes & ConstraintMask;
            GenericParameterAttributes runtimeConstraints =
                runtimeParameter.GenericParameterAttributes & ConstraintMask;
            if ((replacementConstraints & ~runtimeConstraints) != 0)
            {
                return false;
            }

            var runtimeConstraintShapes = new HashSet<string>(
                runtimeParameter.GetGenericParameterConstraints().Select(GetTypeShape));
            return replacementParameter.GetGenericParameterConstraints()
                .All(constraint => runtimeConstraintShapes.Contains(GetTypeShape(constraint)));
        }

        private static bool TypeShapesMatch(Type left, Type right)
        {
#if NET10_0_OR_GREATER
            if (left == typeof(CoyoteLock.Scope) && right == typeof(Lock.Scope))
            {
                return true;
            }
#endif
            return GetTypeShape(left) == GetTypeShape(right);
        }

        private static string GetTypeShape(Type type)
        {
            if (type.IsByRef)
            {
                return GetTypeShape(type.GetElementType()) + "&";
            }

            if (type.IsArray)
            {
                return GetTypeShape(type.GetElementType()) + "[]";
            }

            if (type.IsGenericParameter)
            {
                // Identify generic parameters by their owner kind and position, instead of treating
                // every generic parameter as an indistinguishable wildcard.
                return (type.DeclaringMethod is null ? "!" : "!!") +
                    type.GenericParameterPosition.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (type.IsGenericType)
            {
                return type.GetGenericTypeDefinition().FullName + "<" +
                    string.Join(",", type.GetGenericArguments().Select(GetTypeShape)) + ">";
            }

            return type.FullName;
        }

        private static string GetMethodSignature(MethodInfo method)
        {
            string instanceKind = method.IsStatic ? "static" : "instance";
            string arity = method.IsGenericMethodDefinition ? "``" + method.GetGenericArguments().Length : string.Empty;
            string parameters = string.Join(",", method.GetParameters().Select(parameter => GetTypeShape(parameter.ParameterType)));
            return $"{GetTypeShape(method.DeclaringType)}|{instanceKind}|{method.Name}{arity}({parameters})|{GetTypeShape(method.ReturnType)}";
        }

#if NETFRAMEWORK
        private static Dictionary<string, string> GetUnsupportedSignatures(IEnumerable<ApiMember> members) =>
#else
        private static IReadOnlyDictionary<string, string> GetUnsupportedSignatures(IEnumerable<ApiMember> members) =>
#endif
            members.Where(member => !member.IsSupported).ToDictionary(
                member => member.Signature,
                member => member.PassThroughReason);

        private static class StricterInterlocked
        {
            public static T Exchange<T>(ref T location1, T value)
                where T : class, new() => Interlocked.Exchange(ref location1, value);
        }

        private static class NongenericInterlocked
        {
            public static object Exchange(ref object location1, object value) => Interlocked.Exchange(ref location1, value);
        }

        private sealed class ApiMember
        {
            internal ApiMember(MethodInfo runtimeMethod, Type replacementType, string passThroughReason)
            {
                this.RuntimeMethod = runtimeMethod;
                this.ReplacementType = replacementType;
                this.IsSupported = passThroughReason is null;
                this.PassThroughReason = passThroughReason;
                this.Signature = GetMethodSignature(runtimeMethod);
            }

            internal MethodInfo RuntimeMethod { get; }

            internal Type ReplacementType { get; }

            internal bool IsSupported { get; }

            /// <summary>
            /// The reason why a member that is not supported is deliberately left as a call to the runtime.
            /// </summary>
            internal string PassThroughReason { get; }

            internal string Signature { get; }
        }

        private static class ApiDiffGate
        {
            internal static IReadOnlyList<string> Classify(
                IEnumerable<string> runtimeSignatures,
                IEnumerable<string> supportedSignatures,
                IEnumerable<string> replacementSignatures,
                IReadOnlyDictionary<string, string> unsupportedSignatures)
            {
                var errors = new List<string>();
                var supported = new HashSet<string>(supportedSignatures);
                var replacements = new HashSet<string>(replacementSignatures);
                var runtime = new HashSet<string>(runtimeSignatures);

                foreach (string signature in runtime)
                {
                    if (supported.Contains(signature))
                    {
                        if (!replacements.Contains(signature))
                        {
                            errors.Add($"Missing controlled replacement for runtime signature '{signature}'.");
                        }
                    }
                    else if (unsupportedSignatures.TryGetValue(signature, out string reason))
                    {
                        if (string.IsNullOrWhiteSpace(reason))
                        {
                            errors.Add($"Allowlisted runtime signature '{signature}' must have a nonempty reason.");
                        }
                    }
                    else
                    {
                        errors.Add($"Unclassified runtime signature '{signature}'.");
                    }
                }

                foreach (string signature in supported)
                {
                    if (!runtime.Contains(signature))
                    {
                        errors.Add($"Supported runtime signature '{signature}' is missing from this runtime.");
                    }
                }

                foreach (string signature in unsupportedSignatures.Keys)
                {
                    if (!runtime.Contains(signature))
                    {
                        errors.Add($"Allowlisted runtime signature '{signature}' is missing from this runtime.");
                    }
                }

                return errors;
            }
        }
    }
}
