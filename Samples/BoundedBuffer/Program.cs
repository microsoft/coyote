// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Samples.BoundedBuffer
{
    public static class Program
    {
        private static bool RunningMain = false;

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
            }

            RunningMain = true;

            foreach (var arg in args)
            {
                if (arg[0] == '-')
                {
                    switch (arg.ToUpperInvariant().Trim('-'))
                    {
                        case "M":
                            Console.WriteLine("Running with minimal deadlock...");
                            TestBoundedBufferMinimalDeadlock();
                            break;
                        case "F":
                            Console.WriteLine("Running with no deadlock...");
                            TestBoundedBufferNoDeadlock();
                            break;
                        case "?":
                        case "H":
                        case "HELP":
                            PrintUsage();
                            return;
                        default:
                            Console.WriteLine("### Unknown arg: " + arg);
                            PrintUsage();
                            break;
                    }
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: BoundedBuffer [option]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -m    Run with minimal deadlock");
            Console.WriteLine("  -f    Run fixed version which should not deadlock");
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void TestBoundedBufferFindDeadlockConfiguration(ICoyoteRuntime runtime)
        {
            CheckRewritten();
            var random = Microsoft.Coyote.Random.Generator.Create();
            int bufferSize = random.NextInteger(5) + 1;
            int readers = random.NextInteger(5) + 1;
            int writers = random.NextInteger(5) + 1;
            int iterations = random.NextInteger(10) + 1;
            int totalIterations = iterations * readers;
            int writerIterations = totalIterations / writers;
            int remainder = totalIterations % writers;

            runtime.Logger.WriteLine(LogSeverity.Important, "Testing buffer size {0}, reader={1}, writer={2}, iterations={3}", bufferSize, readers, writers, iterations);

            BoundedBuffer buffer = new BoundedBuffer(bufferSize);
            var tasks = new List<Task>();
            for (int i = 0; i < readers; i++)
            {
                tasks.Add(Task.Run(() => Reader(buffer, iterations)));
            }

            int x = 0;
            for (int i = 0; i < writers; i++)
            {
                int w = writerIterations + ((i == (writers - 1)) ? remainder : 0);
                x += w;
                tasks.Add(Task.Run(() => Writer(buffer, w)));
            }

            Microsoft.Coyote.Specifications.Specification.Assert(x == totalIterations, "total writer iterations doesn't match!");

            Task.WaitAll(tasks.ToArray());
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void TestBoundedBufferMinimalDeadlock()
        {
            CheckRewritten();
            BoundedBuffer buffer = new BoundedBuffer(1);
            var tasks = new List<Task>()
            {
                Task.Run(() => Reader(buffer, 5)),
                Task.Run(() => Reader(buffer, 5)),
                Task.Run(() => Writer(buffer, 10))
            };

            Task.WaitAll(tasks.ToArray());
        }

        private static void Reader(BoundedBuffer buffer, int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                object x = buffer.Take();
            }
        }

        private static void Writer(BoundedBuffer buffer, int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                buffer.Put("hello " + i);
            }
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void TestBoundedBufferNoDeadlock()
        {
            CheckRewritten();
            BoundedBuffer buffer = new BoundedBuffer(1, true);
            var tasks = new List<Task>()
            {
                Task.Run(() => Reader(buffer, 5)),
                Task.Run(() => Reader(buffer, 5)),
                Task.Run(() => Writer(buffer, 10))
            };

            Task.WaitAll(tasks.ToArray());
        }

        private static void CheckRewritten()
        {
            if (!RunningMain && !Microsoft.Coyote.Rewriting.RewritingEngine.IsAssemblyRewritten(typeof(Program).Assembly))
            {
                throw new Exception(string.Format("Error: please rewrite this assembly using coyote rewrite {0}",
                    typeof(Program).Assembly.Location));
            }
        }
    }
}
