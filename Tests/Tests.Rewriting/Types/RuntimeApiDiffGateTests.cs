// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

using CecilMethodDefinition = Mono.Cecil.MethodDefinition;
using CecilModuleDefinition = Mono.Cecil.ModuleDefinition;
using CoyoteEventWaitHandle = Microsoft.Coyote.Rewriting.Types.Threading.EventWaitHandle;
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
    /// <summary>
    /// Gate that discovers the public surface of explicitly audited runtime API families through
    /// reflection, independently of how they are classified, and requires every discovered member
    /// to have exactly one explicit, framework-specific classification.
    /// </summary>
    /// <remarks>
    /// The gate audits the families listed in <see cref="AuditedFamilies"/>, not the entire BCL.
    /// A family is selected by type and member name, so any overload that a newer runtime adds to
    /// an audited family is discovered without changing the discovery configuration, and fails the
    /// gate until it is classified as controlled, rejected or deliberately passed through.
    /// </remarks>
    public class RuntimeApiDiffGateTests : BaseRewritingTest
    {
        private const string RunSynchronouslyReason =
            "Task.RunSynchronously can execute work on an arbitrary scheduler, so rewriting reports it as an " +
            "uncontrolled invocation.";

#if NET10_0_OR_GREATER
        private const string MemoryBarrierReason =
            "A memory barrier neither accesses shared memory nor blocks, so it is invoked natively without a " +
            "scheduling point, like Thread.MemoryBarrier; systematic testing executes one operation at a time " +
            "and does not explore the memory reorderings that the barrier prevents.";

        private const string LockConstructionReason =
            "Constructing a Lock only allocates it; acquiring and releasing it are controlled by the replacement " +
            "through a synchronization object that is associated with the lock on first use.";
#endif

        private const string NamedEventOpeningReason =
            "Opening a named system event is reported as an uncontrolled invocation, because a named event can be " +
            "shared with other processes, which are not controlled.";

        private const string MutexReason =
            "Mutex is a system synchronization object that can be named and shared with other processes, so " +
            "rewriting reports every Mutex member as an uncontrolled invocation.";

        private const string SemaphoreReason =
            "Semaphore is a system synchronization object that can be named and shared with other processes, so " +
            "rewriting reports every Semaphore member as an uncontrolled invocation.";

#if NETFRAMEWORK
        private const string NamedEventSecurityReason =
            "The .NET Framework constructor that applies access control security creates a named system event that " +
            "can be shared with other processes and has no controlled replacement, so rewriting reports it as an " +
            "uncontrolled invocation.";
#endif

        /// <summary>
        /// The explicitly audited runtime API families.
        /// </summary>
        private static readonly AuditedFamily[] AuditedFamilies = new[]
        {
            new AuditedFamily(typeof(Task), typeof(CoyoteTask), false, nameof(Task.Wait), nameof(Task.WaitAll),
                nameof(Task.WaitAny), nameof(Task.WhenAll), nameof(Task.WhenAny), "WhenEach", "WaitAsync",
                nameof(Task.Delay), nameof(Task.RunSynchronously)),
            new AuditedFamily(typeof(Task<>), typeof(Microsoft.Coyote.Rewriting.Types.Threading.Tasks.Task<>), false,
                "WaitAsync"),
            new AuditedFamily(typeof(Monitor), typeof(CoyoteMonitor), false, nameof(Monitor.Enter),
                nameof(Monitor.Exit), nameof(Monitor.TryEnter), nameof(Monitor.Wait), nameof(Monitor.Pulse),
                nameof(Monitor.PulseAll), nameof(Monitor.IsEntered)),
            new AuditedFamily(typeof(SemaphoreSlim), typeof(CoyoteSemaphoreSlim), false, nameof(SemaphoreSlim.Wait),
                nameof(SemaphoreSlim.WaitAsync), nameof(SemaphoreSlim.Release)),
            new AuditedFamily(typeof(Interlocked), typeof(CoyoteInterlocked), false, nameof(Interlocked.Increment),
                nameof(Interlocked.Exchange), nameof(Interlocked.CompareExchange)),
            new AuditedFamily(typeof(Volatile), typeof(CoyoteVolatile), name => name.EndsWith("Barrier", StringComparison.Ordinal)),
            new AuditedFamily(typeof(WaitHandle), typeof(CoyoteWaitHandle), false, nameof(WaitHandle.WaitOne)),
            new AuditedFamily(typeof(Thread), typeof(CoyoteThread), false, nameof(Thread.Sleep)),
#if NET10_0_OR_GREATER
            new AuditedFamily(typeof(Lock), typeof(CoyoteLock), true, name => true),
#endif
            new AuditedFamily(typeof(EventWaitHandle), typeof(CoyoteEventWaitHandle), true,
                nameof(EventWaitHandle.OpenExisting), nameof(EventWaitHandle.TryOpenExisting)),
            new AuditedFamily(typeof(Mutex), null, true, nameof(Mutex.OpenExisting), nameof(Mutex.TryOpenExisting)),
            new AuditedFamily(typeof(Semaphore), null, true, nameof(Semaphore.OpenExisting),
                nameof(Semaphore.TryOpenExisting))
        };

        private static readonly Dictionary<string, CecilModuleDefinition> Modules = new Dictionary<string, CecilModuleDefinition>();

        public RuntimeApiDiffGateTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 10000)]
        public void TestAuditedRuntimeSurfaceIsExactlyClassified()
        {
            IReadOnlyList<string> errors = ApiDiffGate.Check(Discover(AuditedFamilies), GetManifest());
            Assert.True(errors.Count is 0, string.Join(Environment.NewLine, errors));
        }

        [Fact(Timeout = 5000)]
        public void TestUnclassifiedOverloadFoundThroughDiscoveryFailsTheGate()
        {
            // The fixture family is discovered through the same reflection path as the runtime families,
            // and the manifest only classifies one of its two overloads.
            var family = new AuditedFamily(typeof(AuditedFixture), typeof(AuditedFixtureReplacement), false,
                nameof(AuditedFixture.Wait));
            IReadOnlyList<ApiMember> members = Discover(new[] { family });
            Assert.Equal(2, members.Count);

            var manifest = new Manifest().Controlled("AuditedFixture|static|Wait(Int32)|Boolean");
            string error = Assert.Single(ApiDiffGate.Check(members, manifest));
            Assert.Contains("Unclassified runtime member", error);
            Assert.Contains("AuditedFixture|static|Wait(Int32,TimeSpan)|Boolean", error);
        }

        [Fact(Timeout = 5000)]
        public void TestMissingControlledReplacementFailsTheGate()
        {
            var family = new AuditedFamily(typeof(AuditedFixture), typeof(AuditedFixtureReplacement), false,
                nameof(AuditedFixture.Wait));
            var manifest = new Manifest().Controlled(
                "AuditedFixture|static|Wait(Int32)|Boolean",
                "AuditedFixture|static|Wait(Int32,TimeSpan)|Boolean");
            string error = Assert.Single(ApiDiffGate.Check(Discover(new[] { family }), manifest));
            Assert.Contains("Missing controlled replacement", error);
            Assert.Contains("Wait(Int32,TimeSpan)", error);
        }

        [Fact(Timeout = 5000)]
        public void TestStaleClassificationFailsTheGate()
        {
            var family = new AuditedFamily(typeof(AuditedFixture), typeof(AuditedFixtureReplacement), false,
                nameof(AuditedFixture.Wait));
            var manifest = new Manifest()
                .Controlled("AuditedFixture|static|Wait(Int32)|Boolean")
                .PassThrough("Removed from the runtime.", "AuditedFixture|static|Wait(Int32,TimeSpan)|Boolean",
                    "AuditedFixture|static|Wait()|Boolean");
            string error = Assert.Single(ApiDiffGate.Check(Discover(new[] { family }), manifest));
            Assert.Contains("Stale classification", error);
            Assert.Contains("Wait()", error);
        }

        [Fact(Timeout = 5000)]
        public void TestOverlappingClassificationFailsTheGate()
        {
            var family = new AuditedFamily(typeof(AuditedFixture), typeof(AuditedFixtureReplacement), false,
                nameof(AuditedFixture.Wait));
            var manifest = new Manifest()
                .Controlled("AuditedFixture|static|Wait(Int32)|Boolean")
                .PassThrough("Not modeled.", "AuditedFixture|static|Wait(Int32)|Boolean",
                    "AuditedFixture|static|Wait(Int32,TimeSpan)|Boolean");
            IReadOnlyList<string> errors = ApiDiffGate.Check(Discover(new[] { family }), manifest);
            Assert.Contains(errors, error => error.Contains("Overlapping classification") &&
                error.Contains("Wait(Int32)|"));
        }

        [Fact(Timeout = 5000)]
        public void TestClassificationsRequireANonemptyReason()
        {
            var family = new AuditedFamily(typeof(AuditedFixture), typeof(AuditedFixtureReplacement), false,
                nameof(AuditedFixture.Wait));
            var manifest = new Manifest()
                .Controlled("AuditedFixture|static|Wait(Int32)|Boolean")
                .PassThrough(" ", "AuditedFixture|static|Wait(Int32,TimeSpan)|Boolean");
            string error = Assert.Single(ApiDiffGate.Check(Discover(new[] { family }), manifest));
            Assert.Contains("nonempty reason", error);
        }

        [Fact(Timeout = 5000)]
        public void TestRejectedOrPassThroughClassificationMustMatchRewriting()
        {
            // Monitor.Enter has a controlled replacement, so it can be neither passed through nor rejected.
            var family = new AuditedFamily(typeof(Monitor), typeof(CoyoteMonitor), false, nameof(Monitor.Enter));
            IReadOnlyList<ApiMember> members = Discover(new[] { family });
            string[] signatures = members.Select(member => member.Signature).ToArray();
            var passThrough = new Manifest().PassThrough("Not modeled.", signatures);
            Assert.All(ApiDiffGate.Check(members, passThrough), error => Assert.Contains("has a controlled replacement", error));

            // Mutex members are reported as uncontrolled invocations, so they cannot be passed through.
            var mutexFamily = new AuditedFamily(typeof(Mutex), null, true, nameof(Mutex.OpenExisting));
            IReadOnlyList<ApiMember> mutexMembers = Discover(new[] { mutexFamily });
            var mutexManifest = new Manifest().PassThrough("Not modeled.",
                mutexMembers.Select(member => member.Signature).ToArray());
            IReadOnlyList<string> errors = ApiDiffGate.Check(mutexMembers, mutexManifest);
            Assert.NotEmpty(errors);
            Assert.All(errors, error => Assert.Contains("is rejected by rewriting", error));
        }

        [Fact(Timeout = 5000)]
        public void TestReplacementWithStricterGenericConstraintFailsTheGate()
        {
            ApiMember exchange = Discover(AuditedFamilies).Single(member =>
                member.RuntimeMember.DeclaringType == typeof(Interlocked) &&
                member.RuntimeMember.Name == nameof(Interlocked.Exchange) &&
                member.RuntimeMember.IsGenericMethodDefinition);
            Assert.True(HasReplacement(exchange));

            Assert.False(HasReplacement(new ApiMember(exchange.RuntimeMember, typeof(StricterInterlocked))));
            Assert.False(HasReplacement(new ApiMember(exchange.RuntimeMember, typeof(NongenericInterlocked))));

            var manifest = new Manifest().Controlled(exchange.Signature);
            string error = Assert.Single(ApiDiffGate.Check(
                new[] { new ApiMember(exchange.RuntimeMember, typeof(StricterInterlocked)) }, manifest));
            Assert.Contains("Missing controlled replacement", error);
        }

        [Fact(Timeout = 5000)]
        public void TestSignaturesCaptureMemberShape()
        {
            string[] signatures = Discover(AuditedFamilies).Select(member => member.Signature).ToArray();
            Assert.Contains("EventWaitHandle|ctor|.ctor(Boolean,EventResetMode,String,out Boolean)|Void", signatures);
            Assert.Contains("Monitor|static|Enter(Object,ref Boolean)|Void", signatures);
#if NET
            Assert.Contains("Task`1<!0>|instance|WaitAsync(CancellationToken)|Task`1<!0>", signatures);
#endif
            Assert.Contains("Task|instance|Wait()|Void", signatures);
#if NET9_0_OR_GREATER
            Assert.Contains("Interlocked|static|Exchange``1[!!0](ref !!0,!!0)|!!0", signatures);
#else
            Assert.Contains("Interlocked|static|Exchange``1[!!0:class](ref !!0,!!0)|!!0", signatures);
#endif
        }

#if NET10_0_OR_GREATER
        [Fact(Timeout = 5000)]
        public void TestDiscoveryIncludesNet10Overloads()
        {
            string[] signatures = Discover(AuditedFamilies).Select(member => member.Signature).ToArray();
            Assert.Contains("Task|static|WhenEach(ReadOnlySpan`1<Task>)|IAsyncEnumerable`1<Task>", signatures);
            Assert.Contains("Task|static|WhenEach``1[!!0](ReadOnlySpan`1<Task`1<!!0>>)|IAsyncEnumerable`1<Task`1<!!0>>",
                signatures);
            Assert.Contains("Task|static|WhenAll(ReadOnlySpan`1<Task>)|Task", signatures);
            Assert.Contains("Task|static|WhenAny(ReadOnlySpan`1<Task>)|Task`1<Task>", signatures);
            Assert.Contains("Task|static|WaitAll(ReadOnlySpan`1<Task>)|Void", signatures);
            Assert.Contains("Task|static|WaitAll(IEnumerable`1<Task>,CancellationToken)|Void", signatures);
            Assert.Contains("Volatile|static|ReadBarrier()|Void", signatures);
            Assert.Contains("Volatile|static|WriteBarrier()|Void", signatures);
            Assert.Contains("Lock|instance|EnterScope()|Scope", signatures);
            Assert.Contains("Lock|instance|get_IsHeldByCurrentThread()|Boolean", signatures);
            Assert.Contains(
                "EventWaitHandle|ctor|.ctor(Boolean,EventResetMode,String,NamedWaitHandleOptions,out Boolean)|Void",
                signatures);
            Assert.Contains("Mutex|static|OpenExisting(String,NamedWaitHandleOptions)|Mutex", signatures);
        }

        [Fact(Timeout = 5000)]
        public void TestMemoryBarriersAreClassifiedAsPassThrough()
        {
            Manifest manifest = GetManifest();
            ApiMember[] barriers = Discover(AuditedFamilies)
                .Where(member => member.RuntimeMember.DeclaringType == typeof(Volatile))
                .ToArray();
            Assert.Equal(2, barriers.Length);
            Assert.All(barriers, barrier =>
            {
                Manifest.Entry entry = Assert.Single(manifest.Entries, e => e.Signature == barrier.Signature);
                Assert.Equal(Classification.PassThrough, entry.Kind);
                Assert.False(string.IsNullOrWhiteSpace(entry.Reason));
                Assert.False(HasReplacement(barrier));
                Assert.False(IsRejectedByRewriting(barrier));
            });
        }
#endif

        /// <summary>
        /// Returns the explicit classification of every member of the audited families on this framework.
        /// </summary>
        private static Manifest GetManifest()
        {
            var manifest = new Manifest();
            manifest.Controlled(
                "EventWaitHandle|ctor|.ctor(Boolean,EventResetMode,String,out Boolean)|Void",
                "EventWaitHandle|ctor|.ctor(Boolean,EventResetMode,String)|Void",
                "EventWaitHandle|ctor|.ctor(Boolean,EventResetMode)|Void",
                "Interlocked|static|CompareExchange(ref Double,Double,Double)|Double",
                "Interlocked|static|CompareExchange(ref Int32,Int32,Int32)|Int32",
                "Interlocked|static|CompareExchange(ref Int64,Int64,Int64)|Int64",
                "Interlocked|static|CompareExchange(ref IntPtr,IntPtr,IntPtr)|IntPtr",
                "Interlocked|static|CompareExchange(ref Object,Object,Object)|Object",
                "Interlocked|static|CompareExchange(ref Single,Single,Single)|Single",
                "Interlocked|static|Exchange(ref Double,Double)|Double",
                "Interlocked|static|Exchange(ref Int32,Int32)|Int32",
                "Interlocked|static|Exchange(ref Int64,Int64)|Int64",
                "Interlocked|static|Exchange(ref IntPtr,IntPtr)|IntPtr",
                "Interlocked|static|Exchange(ref Object,Object)|Object",
                "Interlocked|static|Exchange(ref Single,Single)|Single",
                "Interlocked|static|Increment(ref Int32)|Int32",
                "Interlocked|static|Increment(ref Int64)|Int64",
                "Monitor|static|Enter(Object,ref Boolean)|Void",
                "Monitor|static|Enter(Object)|Void",
                "Monitor|static|Exit(Object)|Void",
                "Monitor|static|IsEntered(Object)|Boolean",
                "Monitor|static|Pulse(Object)|Void",
                "Monitor|static|PulseAll(Object)|Void",
                "Monitor|static|TryEnter(Object,Int32,ref Boolean)|Void",
                "Monitor|static|TryEnter(Object,Int32)|Boolean",
                "Monitor|static|TryEnter(Object,ref Boolean)|Void",
                "Monitor|static|TryEnter(Object,TimeSpan,ref Boolean)|Void",
                "Monitor|static|TryEnter(Object,TimeSpan)|Boolean",
                "Monitor|static|TryEnter(Object)|Boolean",
                "Monitor|static|Wait(Object,Int32,Boolean)|Boolean",
                "Monitor|static|Wait(Object,Int32)|Boolean",
                "Monitor|static|Wait(Object,TimeSpan,Boolean)|Boolean",
                "Monitor|static|Wait(Object,TimeSpan)|Boolean",
                "Monitor|static|Wait(Object)|Boolean",
                "SemaphoreSlim|instance|Release()|Int32",
                "SemaphoreSlim|instance|Release(Int32)|Int32",
                "SemaphoreSlim|instance|Wait()|Void",
                "SemaphoreSlim|instance|Wait(CancellationToken)|Void",
                "SemaphoreSlim|instance|Wait(Int32,CancellationToken)|Boolean",
                "SemaphoreSlim|instance|Wait(Int32)|Boolean",
                "SemaphoreSlim|instance|Wait(TimeSpan,CancellationToken)|Boolean",
                "SemaphoreSlim|instance|Wait(TimeSpan)|Boolean",
                "SemaphoreSlim|instance|WaitAsync()|Task",
                "SemaphoreSlim|instance|WaitAsync(CancellationToken)|Task",
                "SemaphoreSlim|instance|WaitAsync(Int32,CancellationToken)|Task`1<Boolean>",
                "SemaphoreSlim|instance|WaitAsync(Int32)|Task`1<Boolean>",
                "SemaphoreSlim|instance|WaitAsync(TimeSpan,CancellationToken)|Task`1<Boolean>",
                "SemaphoreSlim|instance|WaitAsync(TimeSpan)|Task`1<Boolean>",
                "Task|instance|Wait()|Void",
                "Task|instance|Wait(CancellationToken)|Void",
                "Task|instance|Wait(Int32,CancellationToken)|Boolean",
                "Task|instance|Wait(Int32)|Boolean",
                "Task|instance|Wait(TimeSpan)|Boolean",
                "Task|static|Delay(Int32,CancellationToken)|Task",
                "Task|static|Delay(Int32)|Task",
                "Task|static|Delay(TimeSpan,CancellationToken)|Task",
                "Task|static|Delay(TimeSpan)|Task",
                "Task|static|WaitAll(Task[],CancellationToken)|Void",
                "Task|static|WaitAll(Task[],Int32,CancellationToken)|Boolean",
                "Task|static|WaitAll(Task[],Int32)|Boolean",
                "Task|static|WaitAll(Task[],TimeSpan)|Boolean",
                "Task|static|WaitAll(Task[])|Void",
                "Task|static|WaitAny(Task[],CancellationToken)|Int32",
                "Task|static|WaitAny(Task[],Int32,CancellationToken)|Int32",
                "Task|static|WaitAny(Task[],Int32)|Int32",
                "Task|static|WaitAny(Task[],TimeSpan)|Int32",
                "Task|static|WaitAny(Task[])|Int32",
                "Task|static|WhenAll(IEnumerable`1<Task>)|Task",
                "Task|static|WhenAll(Task[])|Task",
                "Task|static|WhenAll``1[!!0](IEnumerable`1<Task`1<!!0>>)|Task`1<!!0[]>",
                "Task|static|WhenAll``1[!!0](Task`1<!!0>[])|Task`1<!!0[]>",
                "Task|static|WhenAny(IEnumerable`1<Task>)|Task`1<Task>",
                "Task|static|WhenAny(Task[])|Task`1<Task>",
                "Task|static|WhenAny``1[!!0](IEnumerable`1<Task`1<!!0>>)|Task`1<Task`1<!!0>>",
                "Task|static|WhenAny``1[!!0](Task`1<!!0>[])|Task`1<Task`1<!!0>>",
                "Thread|static|Sleep(Int32)|Void",
                "Thread|static|Sleep(TimeSpan)|Void",
                "WaitHandle|instance|WaitOne()|Boolean",
                "WaitHandle|instance|WaitOne(Int32,Boolean)|Boolean",
                "WaitHandle|instance|WaitOne(Int32)|Boolean",
                "WaitHandle|instance|WaitOne(TimeSpan,Boolean)|Boolean",
                "WaitHandle|instance|WaitOne(TimeSpan)|Boolean");
            manifest.Rejected(MutexReason,
                "Mutex|ctor|.ctor()|Void",
                "Mutex|ctor|.ctor(Boolean,String,out Boolean)|Void",
                "Mutex|ctor|.ctor(Boolean,String)|Void",
                "Mutex|ctor|.ctor(Boolean)|Void",
                "Mutex|static|OpenExisting(String)|Mutex",
                "Mutex|static|TryOpenExisting(String,out Mutex)|Boolean");
            manifest.Rejected(NamedEventOpeningReason,
                "EventWaitHandle|static|OpenExisting(String)|EventWaitHandle",
                "EventWaitHandle|static|TryOpenExisting(String,out EventWaitHandle)|Boolean");
            manifest.Rejected(RunSynchronouslyReason,
                "Task|instance|RunSynchronously()|Void",
                "Task|instance|RunSynchronously(TaskScheduler)|Void");
            manifest.Rejected(SemaphoreReason,
                "Semaphore|ctor|.ctor(Int32,Int32,String,out Boolean)|Void",
                "Semaphore|ctor|.ctor(Int32,Int32,String)|Void",
                "Semaphore|ctor|.ctor(Int32,Int32)|Void",
                "Semaphore|static|OpenExisting(String)|Semaphore",
                "Semaphore|static|TryOpenExisting(String,out Semaphore)|Boolean");

#if NET
            manifest.Controlled(
                "Interlocked|static|CompareExchange(ref UInt32,UInt32,UInt32)|UInt32",
                "Interlocked|static|CompareExchange(ref UInt64,UInt64,UInt64)|UInt64",
                "Interlocked|static|Exchange(ref UInt32,UInt32)|UInt32",
                "Interlocked|static|Exchange(ref UInt64,UInt64)|UInt64",
                "Interlocked|static|Increment(ref UInt32)|UInt32",
                "Interlocked|static|Increment(ref UInt64)|UInt64",
                "Task`1<!0>|instance|WaitAsync(CancellationToken)|Task`1<!0>",
                "Task`1<!0>|instance|WaitAsync(TimeSpan,CancellationToken)|Task`1<!0>",
                "Task`1<!0>|instance|WaitAsync(TimeSpan)|Task`1<!0>",
                "Task|instance|WaitAsync(CancellationToken)|Task",
                "Task|instance|WaitAsync(TimeSpan,CancellationToken)|Task",
                "Task|instance|WaitAsync(TimeSpan)|Task",
                "Task|static|WhenAny(Task,Task)|Task`1<Task>",
                "Task|static|WhenAny``1[!!0](Task`1<!!0>,Task`1<!!0>)|Task`1<Task`1<!!0>>");
#endif

#if NET8_0_OR_GREATER
            manifest.Controlled(
                "Interlocked|static|CompareExchange(ref UIntPtr,UIntPtr,UIntPtr)|UIntPtr",
                "Interlocked|static|Exchange(ref UIntPtr,UIntPtr)|UIntPtr",
                "Task`1<!0>|instance|WaitAsync(TimeSpan,TimeProvider,CancellationToken)|Task`1<!0>",
                "Task`1<!0>|instance|WaitAsync(TimeSpan,TimeProvider)|Task`1<!0>",
                "Task|instance|Wait(TimeSpan,CancellationToken)|Boolean",
                "Task|instance|WaitAsync(TimeSpan,TimeProvider,CancellationToken)|Task",
                "Task|instance|WaitAsync(TimeSpan,TimeProvider)|Task",
                "Task|static|Delay(TimeSpan,TimeProvider,CancellationToken)|Task",
                "Task|static|Delay(TimeSpan,TimeProvider)|Task");
#endif

#if NET10_0_OR_GREATER
            manifest.Controlled(
                "EventWaitHandle|ctor|.ctor(Boolean,EventResetMode,String,NamedWaitHandleOptions,out Boolean)|Void",
                "EventWaitHandle|ctor|.ctor(Boolean,EventResetMode,String,NamedWaitHandleOptions)|Void",
                "Interlocked|static|CompareExchange(ref Byte,Byte,Byte)|Byte",
                "Interlocked|static|CompareExchange(ref Int16,Int16,Int16)|Int16",
                "Interlocked|static|CompareExchange(ref SByte,SByte,SByte)|SByte",
                "Interlocked|static|CompareExchange(ref UInt16,UInt16,UInt16)|UInt16",
                "Interlocked|static|CompareExchange``1[!!0](ref !!0,!!0,!!0)|!!0",
                "Interlocked|static|Exchange(ref Byte,Byte)|Byte",
                "Interlocked|static|Exchange(ref Int16,Int16)|Int16",
                "Interlocked|static|Exchange(ref SByte,SByte)|SByte",
                "Interlocked|static|Exchange(ref UInt16,UInt16)|UInt16",
                "Interlocked|static|Exchange``1[!!0](ref !!0,!!0)|!!0",
                "Lock|instance|Enter()|Void",
                "Lock|instance|EnterScope()|Scope",
                "Lock|instance|Exit()|Void",
                "Lock|instance|get_IsHeldByCurrentThread()|Boolean",
                "Lock|instance|TryEnter()|Boolean",
                "Lock|instance|TryEnter(Int32)|Boolean",
                "Lock|instance|TryEnter(TimeSpan)|Boolean",
                "Task|static|WaitAll(IEnumerable`1<Task>,CancellationToken)|Void",
                "Task|static|WaitAll(ReadOnlySpan`1<Task>)|Void",
                "Task|static|WhenAll(ReadOnlySpan`1<Task>)|Task",
                "Task|static|WhenAll``1[!!0](ReadOnlySpan`1<Task`1<!!0>>)|Task`1<!!0[]>",
                "Task|static|WhenAny(ReadOnlySpan`1<Task>)|Task`1<Task>",
                "Task|static|WhenAny``1[!!0](ReadOnlySpan`1<Task`1<!!0>>)|Task`1<Task`1<!!0>>",
                "Task|static|WhenEach(IEnumerable`1<Task>)|IAsyncEnumerable`1<Task>",
                "Task|static|WhenEach(ReadOnlySpan`1<Task>)|IAsyncEnumerable`1<Task>",
                "Task|static|WhenEach(Task[])|IAsyncEnumerable`1<Task>",
                "Task|static|WhenEach``1[!!0](IEnumerable`1<Task`1<!!0>>)|IAsyncEnumerable`1<Task`1<!!0>>",
                "Task|static|WhenEach``1[!!0](ReadOnlySpan`1<Task`1<!!0>>)|IAsyncEnumerable`1<Task`1<!!0>>",
                "Task|static|WhenEach``1[!!0](Task`1<!!0>[])|IAsyncEnumerable`1<Task`1<!!0>>");
            manifest.PassThrough(LockConstructionReason,
                "Lock|ctor|.ctor()|Void");
            manifest.PassThrough(MemoryBarrierReason,
                "Volatile|static|ReadBarrier()|Void",
                "Volatile|static|WriteBarrier()|Void");
            manifest.Rejected(MutexReason,
                "Mutex|ctor|.ctor(Boolean,String,NamedWaitHandleOptions,out Boolean)|Void",
                "Mutex|ctor|.ctor(Boolean,String,NamedWaitHandleOptions)|Void",
                "Mutex|ctor|.ctor(String,NamedWaitHandleOptions)|Void",
                "Mutex|static|OpenExisting(String,NamedWaitHandleOptions)|Mutex",
                "Mutex|static|TryOpenExisting(String,NamedWaitHandleOptions,out Mutex)|Boolean");
            manifest.Rejected(NamedEventOpeningReason,
                "EventWaitHandle|static|OpenExisting(String,NamedWaitHandleOptions)|EventWaitHandle",
                "EventWaitHandle|static|TryOpenExisting(String,NamedWaitHandleOptions,out EventWaitHandle)|Boolean");
            manifest.Rejected(SemaphoreReason,
                "Semaphore|ctor|.ctor(Int32,Int32,String,NamedWaitHandleOptions,out Boolean)|Void",
                "Semaphore|ctor|.ctor(Int32,Int32,String,NamedWaitHandleOptions)|Void",
                "Semaphore|static|OpenExisting(String,NamedWaitHandleOptions)|Semaphore",
                "Semaphore|static|TryOpenExisting(String,NamedWaitHandleOptions,out Semaphore)|Boolean");
#endif

#if !NET9_0_OR_GREATER
            manifest.Controlled(
                "Interlocked|static|CompareExchange``1[!!0:class](ref !!0,!!0,!!0)|!!0",
                "Interlocked|static|Exchange``1[!!0:class](ref !!0,!!0)|!!0");
#endif

#if NETFRAMEWORK
            manifest.Rejected(MutexReason,
                "Mutex|ctor|.ctor(Boolean,String,out Boolean,MutexSecurity)|Void",
                "Mutex|static|OpenExisting(String,MutexRights)|Mutex",
                "Mutex|static|TryOpenExisting(String,MutexRights,out Mutex)|Boolean");
            manifest.Rejected(NamedEventOpeningReason,
                "EventWaitHandle|static|OpenExisting(String,EventWaitHandleRights)|EventWaitHandle",
                "EventWaitHandle|static|TryOpenExisting(String,EventWaitHandleRights,out EventWaitHandle)|Boolean");
            manifest.Rejected(NamedEventSecurityReason,
                "EventWaitHandle|ctor|.ctor(Boolean,EventResetMode,String,out Boolean,EventWaitHandleSecurity)|Void");
            manifest.Rejected(SemaphoreReason,
                "Semaphore|ctor|.ctor(Int32,Int32,String,out Boolean,SemaphoreSecurity)|Void",
                "Semaphore|static|OpenExisting(String,SemaphoreRights)|Semaphore",
                "Semaphore|static|TryOpenExisting(String,SemaphoreRights,out Semaphore)|Boolean");
#endif

            return manifest;
        }

        /// <summary>
        /// Discovers the public members of the specified families through reflection.
        /// </summary>
        private static IReadOnlyList<ApiMember> Discover(IEnumerable<AuditedFamily> families)
        {
            var members = new List<ApiMember>();
            foreach (AuditedFamily family in families)
            {
                const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.DeclaredOnly;
                IEnumerable<MethodBase> candidates = family.RuntimeType.GetMethods(Flags)
                    .Where(method => family.IncludesMember(method.Name));
                if (family.IncludesConstructors)
                {
                    candidates = candidates.Concat(family.RuntimeType.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
                }

                members.AddRange(candidates.Select(member => new ApiMember(member, family.ReplacementType)));
            }

            return members;
        }

        private static bool HasReplacement(ApiMember member)
        {
            if (member.ReplacementType is null)
            {
                return false;
            }

            MethodBase runtimeMember = member.RuntimeMember;
            bool isConstructor = runtimeMember is ConstructorInfo;
            string replacementName = isConstructor ? "Create" : runtimeMember.Name;
            Type runtimeReturnType = isConstructor ? runtimeMember.DeclaringType : ((MethodInfo)runtimeMember).ReturnType;
            int offset = runtimeMember.IsStatic || isConstructor ? 0 : 1;
            ParameterInfo[] runtimeParameters = runtimeMember.GetParameters();
            foreach (MethodInfo replacementMethod in member.ReplacementType.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (replacementMethod.Name != replacementName)
                {
                    continue;
                }

                ParameterInfo[] replacementParameters = replacementMethod.GetParameters();
                if (replacementParameters.Length != runtimeParameters.Length + offset ||
                    !GenericParametersMatch(replacementMethod, runtimeMember) ||
                    !TypeShapesMatch(replacementMethod.ReturnType, runtimeReturnType))
                {
                    continue;
                }

                if (offset is 1 && !TypeShapesMatch(replacementParameters[0].ParameterType, runtimeMember.DeclaringType))
                {
                    continue;
                }

                bool matched = true;
                for (int idx = 0; idx < runtimeParameters.Length; idx++)
                {
                    if (GetParameterShape(replacementParameters[idx + offset]) != GetParameterShape(runtimeParameters[idx]))
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
        /// Checks if the rewriter reports invocations of the member as uncontrolled, using the same check
        /// as the uncontrolled invocation rewriting pass on the runtime metadata of the member.
        /// </summary>
        private static bool IsRejectedByRewriting(ApiMember member)
        {
            CecilModuleDefinition module;
            lock (Modules)
            {
                string path = member.RuntimeMember.Module.FullyQualifiedName;
                if (!Modules.TryGetValue(path, out module))
                {
                    module = CecilModuleDefinition.ReadModule(path);
                    Modules.Add(path, module);
                }
            }

            var definition = (CecilMethodDefinition)module.LookupToken(member.RuntimeMember.MetadataToken);
            MethodInfo check = typeof(UncontrolledInvocationRewritingPass).GetMethod("IsUncontrolledType",
                BindingFlags.NonPublic | BindingFlags.Static);
            object[] arguments = new object[] { definition.DeclaringType, definition, false };
            return (bool)check.Invoke(null, arguments);
        }

        /// <summary>
        /// Checks that the replacement has the same generic arity as the runtime method, and that
        /// every generic argument that is valid for the runtime method is valid for the replacement,
        /// so that rewriting a runtime-valid generic call cannot violate a stricter constraint.
        /// </summary>
        private static bool GenericParametersMatch(MethodInfo replacementMethod, MethodBase runtimeMember)
        {
            Type[] replacementParameters = replacementMethod.IsGenericMethodDefinition ?
                replacementMethod.GetGenericArguments() : Type.EmptyTypes;
            Type[] runtimeParameters = runtimeMember.IsGenericMethodDefinition ?
                runtimeMember.GetGenericArguments() : Type.EmptyTypes;
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
            GenericParameterAttributes replacementConstraints =
                replacementParameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
            GenericParameterAttributes runtimeConstraints =
                runtimeParameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
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

        /// <summary>
        /// Returns the shape of a type: types are identified by name, generic parameters by their owner
        /// kind and position, instead of treating every generic parameter as an indistinguishable wildcard.
        /// </summary>
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
                return (type.DeclaringMethod is null ? "!" : "!!") +
                    type.GenericParameterPosition.ToString(CultureInfo.InvariantCulture);
            }

            if (type.IsGenericType)
            {
                return type.GetGenericTypeDefinition().Name + "<" +
                    string.Join(",", type.GetGenericArguments().Select(GetTypeShape)) + ">";
            }

            return type.Name;
        }

        private static string GetParameterShape(ParameterInfo parameter)
        {
            Type type = parameter.ParameterType;
            if (!type.IsByRef)
            {
                return GetTypeShape(type);
            }

            string modifier = parameter.IsOut ? "out " : parameter.IsIn ? "in " : "ref ";
            return modifier + GetTypeShape(type.GetElementType());
        }

        private static string GetGenericShape(MethodBase member)
        {
            if (!member.IsGenericMethodDefinition)
            {
                return string.Empty;
            }

            Type[] arguments = member.GetGenericArguments();
            IEnumerable<string> parameters = arguments.Select(argument =>
            {
                var constraints = new List<string>();
                GenericParameterAttributes attributes = argument.GenericParameterAttributes;
                if ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                {
                    constraints.Add("class");
                }

                if ((attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                {
                    constraints.Add("struct");
                }
                else if ((attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
                {
                    constraints.Add("new()");
                }

                constraints.AddRange(argument.GetGenericParameterConstraints()
                    .Where(constraint => constraint != typeof(ValueType))
                    .Select(GetTypeShape));
                string shape = GetTypeShape(argument);
                return constraints.Count is 0 ? shape : shape + ":" + string.Join(",", constraints);
            });

            return "``" + arguments.Length.ToString(CultureInfo.InvariantCulture) + "[" + string.Join(";", parameters) + "]";
        }

        /// <summary>
        /// Returns the signature of a member, which captures its declaring type, static, instance or
        /// constructor form, name, generic arity and constraints, parameter types with their ref, out
        /// or in modifiers, and return type.
        /// </summary>
        private static string GetSignature(MethodBase member)
        {
            string kind = member is ConstructorInfo ? "ctor" : member.IsStatic ? "static" : "instance";
            string parameters = string.Join(",", member.GetParameters().Select(GetParameterShape));
            string returnType = member is MethodInfo method ? GetTypeShape(method.ReturnType) : "Void";
            return $"{GetTypeShape(member.DeclaringType)}|{kind}|{member.Name}{GetGenericShape(member)}({parameters})|{returnType}";
        }

        /// <summary>
        /// The classification of an audited runtime member.
        /// </summary>
        private enum Classification
        {
            /// <summary>
            /// The member is rewritten to a controlled replacement.
            /// </summary>
            Controlled,

            /// <summary>
            /// The member is reported as an uncontrolled invocation by the rewriter.
            /// </summary>
            Rejected,

            /// <summary>
            /// The member is deliberately left as a native call that needs no control.
            /// </summary>
            PassThrough
        }

        /// <summary>
        /// A family of audited runtime members that are selected by type and member name.
        /// </summary>
        private sealed class AuditedFamily
        {
            internal AuditedFamily(Type runtimeType, Type replacementType, bool includesConstructors,
                params string[] memberNames)
                : this(runtimeType, replacementType, includesConstructors, new HashSet<string>(memberNames).Contains)
            {
            }

            internal AuditedFamily(Type runtimeType, Type replacementType, Func<string, bool> includesMember)
                : this(runtimeType, replacementType, false, includesMember)
            {
            }

            internal AuditedFamily(Type runtimeType, Type replacementType, bool includesConstructors,
                Func<string, bool> includesMember)
            {
                this.RuntimeType = runtimeType;
                this.ReplacementType = replacementType;
                this.IncludesConstructors = includesConstructors;
                this.IncludesMember = includesMember;
            }

            internal Type RuntimeType { get; }

            internal Type ReplacementType { get; }

            internal bool IncludesConstructors { get; }

            internal Func<string, bool> IncludesMember { get; }
        }

        /// <summary>
        /// A discovered runtime member.
        /// </summary>
        private sealed class ApiMember
        {
            internal ApiMember(MethodBase runtimeMember, Type replacementType)
            {
                this.RuntimeMember = runtimeMember;
                this.ReplacementType = replacementType;
                this.Signature = GetSignature(runtimeMember);
            }

            internal MethodBase RuntimeMember { get; }

            internal Type ReplacementType { get; }

            internal string Signature { get; }
        }

        /// <summary>
        /// The explicit classifications of audited runtime members.
        /// </summary>
        private sealed class Manifest
        {
            internal List<Entry> Entries { get; } = new List<Entry>();

            internal Manifest Controlled(params string[] signatures) =>
                this.Add(Classification.Controlled, null, signatures);

            internal Manifest Rejected(string reason, params string[] signatures) =>
                this.Add(Classification.Rejected, reason, signatures);

            internal Manifest PassThrough(string reason, params string[] signatures) =>
                this.Add(Classification.PassThrough, reason, signatures);

            private Manifest Add(Classification kind, string reason, string[] signatures)
            {
                this.Entries.AddRange(signatures.Select(signature => new Entry(signature, kind, reason)));
                return this;
            }

            internal sealed class Entry
            {
                internal Entry(string signature, Classification kind, string reason)
                {
                    this.Signature = signature;
                    this.Kind = kind;
                    this.Reason = reason;
                }

                internal string Signature { get; }

                internal Classification Kind { get; }

                internal string Reason { get; }
            }
        }

        private static class ApiDiffGate
        {
            internal static IReadOnlyList<string> Check(IEnumerable<ApiMember> discovered, Manifest manifest)
            {
                var errors = new List<string>();
                var classifications = new Dictionary<string, Manifest.Entry>();
                foreach (Manifest.Entry entry in manifest.Entries)
                {
                    if (classifications.ContainsKey(entry.Signature))
                    {
                        errors.Add($"Overlapping classification of runtime member '{entry.Signature}'.");
                        continue;
                    }

                    classifications.Add(entry.Signature, entry);
                    if (entry.Kind != Classification.Controlled && string.IsNullOrWhiteSpace(entry.Reason))
                    {
                        errors.Add($"Runtime member '{entry.Signature}' classified as {entry.Kind} must have a nonempty reason.");
                    }
                }

                var discoveredSignatures = new HashSet<string>();
                foreach (ApiMember member in discovered)
                {
                    if (!discoveredSignatures.Add(member.Signature))
                    {
                        errors.Add($"Runtime member signature '{member.Signature}' is ambiguous.");
                        continue;
                    }

                    if (!classifications.TryGetValue(member.Signature, out Manifest.Entry entry))
                    {
                        errors.Add($"Unclassified runtime member '{member.Signature}'.");
                        continue;
                    }

                    bool hasReplacement = HasReplacement(member);
                    if (entry.Kind is Classification.Controlled)
                    {
                        if (!hasReplacement)
                        {
                            errors.Add($"Missing controlled replacement for runtime member '{member.Signature}'.");
                        }

                        continue;
                    }

                    if (hasReplacement)
                    {
                        errors.Add($"Runtime member '{member.Signature}' is classified as {entry.Kind}, " +
                            "but it has a controlled replacement.");
                    }

                    bool isRejected = IsRejectedByRewriting(member);
                    if (entry.Kind is Classification.Rejected && !isRejected)
                    {
                        errors.Add($"Runtime member '{member.Signature}' is classified as Rejected, " +
                            "but it is not rejected by rewriting.");
                    }
                    else if (entry.Kind is Classification.PassThrough && isRejected)
                    {
                        errors.Add($"Runtime member '{member.Signature}' is classified as PassThrough, " +
                            "but it is rejected by rewriting.");
                    }
                }

                foreach (string signature in classifications.Keys.Where(signature => !discoveredSignatures.Contains(signature)))
                {
                    errors.Add($"Stale classification of runtime member '{signature}', which is not discovered on this runtime.");
                }

                return errors;
            }
        }

        /// <summary>
        /// Audited fixture type that stands in for a runtime type to which a new overload was added.
        /// </summary>
        private static class AuditedFixture
        {
            public static bool Wait(int millisecondsTimeout) => millisecondsTimeout > 0;

            public static bool Wait(int millisecondsTimeout, TimeSpan timeout) => millisecondsTimeout > 0 && timeout > TimeSpan.Zero;
        }

        private static class AuditedFixtureReplacement
        {
            public static bool Wait(int millisecondsTimeout) => AuditedFixture.Wait(millisecondsTimeout);
        }

        private static class StricterInterlocked
        {
            public static T Exchange<T>(ref T location1, T value)
                where T : class, new() => Interlocked.Exchange(ref location1, value);
        }

        private static class NongenericInterlocked
        {
            public static object Exchange(ref object location1, object value) => Interlocked.Exchange(ref location1, value);
        }
    }
}
