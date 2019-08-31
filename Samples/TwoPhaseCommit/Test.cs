using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Runtime;

namespace TwoPhaseCommit
{
    /// <summary>
    /// A single-process implementation of the two phase commit protocol written using
    /// C# and the Coyote library.
    ///
    /// A description of the two phase commit protocol is available here:
    /// https://en.wikipedia.org/wiki/Two-phase_commit_protocol
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
            runtime.CreateMachine(typeof(TwoPhaseCommit));
        }
    }
}
