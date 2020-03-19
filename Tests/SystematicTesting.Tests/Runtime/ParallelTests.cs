// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;

namespace Microsoft.Coyote.SystematicTesting.Tests.Runtime
{
    public class ParallelTests
    {
        [Fact(Timeout = 5000)]
        public void TestParallelTestEngines()
        {
            string tempDir = Path.GetTempPath();
            string outdir1 = Path.Combine(tempDir, "Coyote", "test1") + "\\";
            string outdir2 = Path.Combine(tempDir, "Coyote", "test2") + "\\";
            string outdir3 = Path.Combine(tempDir, "Coyote", "test3") + "\\";

            SafeDeleteDirectory(outdir1);
            SafeDeleteDirectory(outdir2);
            SafeDeleteDirectory(outdir3);

            var task1 = Task.Run(() =>
            {
                RunTest(PingPongClient.RunTest, outdir1);
            });
            var task2 = Task.Run(() =>
            {
                RunTest(PingPongClient.RunTest, outdir2);
            });
            var task3 = Task.Run(() =>
            {
                RunTest(PingPongClient.RunTest, outdir3);
            });

            Task.WaitAll(task1, task2, task3);

            string[] expected = new string[] { "PingPongClient", "PingPongServer", "LivenessMonitor", "Init", "WaitForPong", "Pong", "Busy", "Idle" };
            AssertFileContainsKeywords(Path.Combine(outdir1, "foo_0.dgml"), expected);
        }

        private static void AssertFileContainsKeywords(string filename, string[] keywords)
        {
            string text = File.ReadAllText(filename);
            foreach (string keyword in keywords)
            {
                Assert.True(text.Contains(keyword), string.Format("missing keyword '{0}' in output file: {1}", keyword, filename));
            }
        }

        private static void SafeDeleteDirectory(string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }

        private static void RunTest(Action<IActorRuntime> test, string outdir)
        {
            Directory.CreateDirectory(outdir);
            Configuration conf = Configuration.Create().WithTestingIterations(10).WithPCTStrategy(false, 10).WithActivityCoverageEnabled();
            TestingEngine engine = TestingEngine.Create(conf, test);
            engine.Run();

            System.Threading.Thread.Sleep(2000);
            foreach (var name in engine.TryEmitTraces(outdir, "foo"))
            {
                Console.WriteLine(name);
            }

            Console.WriteLine("Test complete");
        }

        [OnEventDoAction(typeof(PingPongClient.PingEvent), nameof(OnPing))]
        public class PingPongServer : Actor
        {
            public class PongEvent : Event
            {
            }

            private void OnPing(Event e)
            {
                if (e is PingPongClient.PingEvent pe)
                {
                    this.Logger.WriteLine("Server Sending Pong");
                    this.SendEvent(pe.Client, new PongEvent());
                }
            }
        }

        public class PingPongClient : StateMachine
        {
            public class PingEvent : Event
            {
                public ActorId Client;
            }

            public class ConfigEvent : Event
            {
                public ActorId Server;
            }

            private ActorId ServerId;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            private class Init : State
            {
            }

            private void OnInitEntry(Event e)
            {
                if (e is ConfigEvent ce)
                {
                    this.ServerId = ce.Server;
                }

                this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
                this.Logger.WriteLine("Client Sending ping");
                this.SendEvent(this.ServerId, new PingEvent() { Client = this.Id });
                this.RaiseGotoStateEvent<WaitForPong>();
            }

            [OnEntry(nameof(OnWaitForPong))]
            [OnEventGotoState(typeof(PingPongServer.PongEvent), typeof(Pong))]
            private class WaitForPong : State
            {
            }

            private void OnWaitForPong()
            {
                this.Logger.WriteLine("Client waiting for pong");
            }

            [OnEntry(nameof(OnPongEntry))]
            private class Pong : State
            {
            }

            private void OnPongEntry()
            {
                this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
                this.Logger.WriteLine("Pong received");
            }

            public static void RunTest(IActorRuntime runtime)
            {
                runtime.RegisterMonitor(typeof(LivenessMonitor));
                ActorId server = runtime.CreateActor(typeof(PingPongServer));
                runtime.CreateActor(typeof(PingPongClient), new PingPongClient.ConfigEvent() { Server = server });
            }
        }

        private class LivenessMonitor : Monitor
        {
            public class BusyEvent : Event
            {
            }

            public class IdleEvent : Event
            {
            }

            [Start]
            [Cold]
            [IgnoreEvents(typeof(IdleEvent))]
            [OnEventGotoState(typeof(BusyEvent), typeof(Busy))]

            private class Idle : State
            {
            }

            [Hot]
            [IgnoreEvents(typeof(BusyEvent))]
            [OnEventGotoState(typeof(IdleEvent), typeof(Idle))]

            private class Busy : State
            {
            }
        }
    }
}
