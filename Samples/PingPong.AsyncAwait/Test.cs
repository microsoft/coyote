using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Runtime;

namespace PingPong.AsyncAwait
{
    /// <summary>
    /// A simple PingPong application written using C# and the Coyote library.
    ///
    /// The Coyote runtime starts by creating the Coyote machine 'NetworkEnvironment'. The
    /// 'NetworkEnvironment' machine then creates a 'Server' and a 'Client' machine,
    /// which then communicate by sending 'Ping' and 'Pong' events to each other for
    /// a limited amount of turns.
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

        /// <summary>
        /// The Coyote testing engine uses a method annotated with the 'Microsoft.Coyote.Test'
        /// attribute as an entry point.
        ///
        /// During testing, the testing engine takes control of the underlying scheduler
        /// and any declared in Coyote sources of non-determinism (e.g. Coyote asynchronous APIs,
        /// Coyote non-determinstic choices) and systematically executes the test method a user
        /// specified number of iterations to detect bugs.
        /// </summary>
        /// <param name="runtime">The machine runtime.</param>
        [Microsoft.Coyote.Test]
        public static void Execute(ICoyoteRuntime runtime)
        {
            // This is the root machine to the PingPong program. CreateMachine
            // executes asynchronously (i.e. non-blocking).
            runtime.CreateMachine(typeof(NetworkEnvironment));
        }
    }
}
