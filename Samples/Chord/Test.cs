using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Runtime;

namespace Chord
{
    /// <summary>
    /// A single-process implementation of the chord peer-to-peer look up service written
    /// using C# and the Coyote library.
    ///
    /// The Chord protocol is described in the following paper:
    /// https://pdos.csail.mit.edu/papers/chord:sigcomm01/chord_sigcomm.pdf
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
            runtime.CreateMachine(typeof(ClusterManager));
        }
    }
}
