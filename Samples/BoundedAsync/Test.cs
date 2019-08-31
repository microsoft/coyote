using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Runtime;

namespace BoundedAsync
{
    /// <summary>
    /// A sample application written using C# and the Coyote library.
    ///
    /// The Coyote runtime starts by creating the Coyote machine 'Scheduler'. The 'Scheduler' machine
    /// then creates a user-defined number of 'Process' machines, which communicate with each
    /// other by exchanging a 'count' value. The processes assert that their count value is
    /// always equal (or minus one) to their neighbour's count value.
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
            runtime.CreateMachine(typeof(Scheduler), new Scheduler.Config(3));
        }
    }
}
