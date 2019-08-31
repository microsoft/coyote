using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Runtime;

namespace ReplicatingStorage
{
    /// <summary>
    /// A single-process implementation of a replicating storage written using C# and the
    /// Coyote library.
    ///
    /// This is a (much) simplified version of the system described in the following paper:
    /// https://www.usenix.org/system/files/conference/fast16/fast16-papers-deligiannis.pdf
    ///
    /// The liveness bug (discussed in the paper) is injected in:
    ///     NodeManager.cs, line 181
    ///
    /// Note: this is an abstract implementation aimed primarily to showcase the testing
    /// capabilities of Coyote.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // Optional: increases verbosity level to see the Coyote runtime log.
            var configuration = Configuration.Create().WithVerbosityEnabled();

            // Creates a new Coyote runtime instance, and passes an optional configuration.
            var runtime = CoyoteRuntime.Create(configuration);

            // Executes the Coyote program.
            Program.Execute(runtime);

            // The Coyote runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }

        [Microsoft.Coyote.Test]
        public static void Execute(ICoyoteRuntime runtime)
        {
            runtime.RegisterMonitor(typeof(LivenessMonitor));
            runtime.CreateMachine(typeof(Environment));
        }
    }
}
