// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// This program runs the Raft service using an Azure Service Bus.
    /// This Program is used to launch the Client and a number of Server instances
    /// in separate processes.
    /// The presence of the --server-id and --client-process-id arguments makes
    /// a server instance.  The Client instance starts the number of servers specified
    /// in the --num-servers argument.
    /// </summary>
    public class Program
    {
        private static readonly List<Process> ServerProcesses = new List<Process>();

        private ActorId LocalId;  // id of local Client or Server actor.
        private string ConnectionString = string.Empty;
        private string TopicName = "rafttopic";
        private int NumRequests = -1;
        private int ClusterSize = -1;
        private int ServerId = -1;
        private int ClientProcessId = -1;
        private readonly bool Debug = false;
        private TaskCompletionSource<ClientResponseEvent> completed;

        internal static void PrintUsage()
        {
            Console.WriteLine("Usage: Raft.AzureClient <options>");
            Console.WriteLine("This program starts the Azure Client process and a set of child processes for each server in the cluster. " +
                              "It then kicks things off by telling the Client machine to send a number of requests to be handled by the " +
                              "elected Leader in the Server cluster");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("     --connection-string     your Azure Service Bus connection string");
            Console.WriteLine("     --topic-name            optional string for Service Bus Topic (default 'rafttopic')");
            Console.WriteLine("     --num-servers           number of servers to spawn");
        }

        private bool ParseCommandLine(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--connection-string":
                        this.ConnectionString = args[++i];
                        break;

                    case "--topic-name":
                        this.TopicName = args[++i];
                        break;

                    case "--local-cluster-size":
                        if (int.TryParse(args[++i], out int localClusterSizeValue))
                        {
                            this.ClusterSize = localClusterSizeValue;
                        }

                        break;

                    case "--num-requests":
                        if (int.TryParse(args[++i], out int numRequestsValue))
                        {
                            this.NumRequests = numRequestsValue;
                        }

                        break;

                    case "--server-id":
                        if (int.TryParse(args[++i], out int serverIdValue))
                        {
                            this.ServerId = serverIdValue;
                        }

                        break;

                    case "--client-process-id":
                        if (int.TryParse(args[++i], out int clientProcessIdValue))
                        {
                            this.ClientProcessId = clientProcessIdValue;
                        }

                        break;

                    case "--debug":
                        Console.WriteLine("debug process " + Process.GetCurrentProcess().Id);
                        Task.Delay(10000).Wait();
                        break;

                    case "--?":
                    case "--help":
                    case "-?":
                    case "-help":
                    case "help":
                    case "?":
                        return false;

                    default:
                        Console.WriteLine("Error: unknown argument: " + args[i]);
                        return false;
                }
            }

            if (string.IsNullOrEmpty(this.ConnectionString))
            {
                Console.WriteLine("Error: please specify the --connection-string argument");
                return false;
            }
            else if (string.IsNullOrEmpty(this.TopicName))
            {
                Console.WriteLine("Error: please specify the --topic-name argument");
                return false;
            }
            else if (this.ClusterSize < 3)
            {
                Console.WriteLine("Error: please specify a valid --local-cluster-size argument > 3");
                return false;
            }

            return true;
        }

        public static async Task Main(string[] args)
        {
            var p = new Program();
            if (p.ParseCommandLine(args))
            {
                await p.RunAsync();
            }
            else
            {
                PrintUsage();
            }
        }

        internal async Task RunAsync()
        {
            try
            {
                // We use the Topic/Subscription pattern which is a pub/sub model where each Server will send
                // messages to this Topic, and every other server has a Subscription to receive those messages.
                // If the Message has a "To" field then the server ignores the message as it was not meant for them.
                // Otherwise the Message is considered a "broadcast" to all servers and each server will handle it.
                // The client also has a subcription on the same topic and is how it broadcasts requests and receives
                // the final response from the elected Leader.
                var managementClient = new ManagementClient(this.ConnectionString);
                if (!await managementClient.TopicExistsAsync(this.TopicName))
                {
                    await managementClient.CreateTopicAsync(this.TopicName);
                }

                // then we need a subscription, whether we are client or server and the subscription name will be
                // the same as our local actorid.
                string subscriptionName = (this.ServerId < 0) ? "Client" : $"Server-{this.ServerId}";
                if (!await managementClient.SubscriptionExistsAsync(this.TopicName, subscriptionName))
                {
                    await managementClient.CreateSubscriptionAsync(
                        new SubscriptionDescription(this.TopicName, subscriptionName));
                }

                Console.WriteLine("Running " + subscriptionName);

                IActorRuntime runtime = RuntimeFactory.Create(Configuration.Create().WithVerbosityEnabled());

                // We create a new Coyote actor runtime instance, and pass an optional configuration
                // that increases the verbosity level to see the Coyote runtime log.
                runtime.OnFailure += RuntimeOnFailure;

                var topicClient = new TopicClient(this.ConnectionString, this.TopicName);

                // cluster manager needs the topic client in order to be able to broadcast messages using Azure Service Bus
                var clusterManager = runtime.CreateActor(typeof(AzureClusterManager), new AzureClusterManager.RegisterMessageBusEvent() { TopicClient = topicClient });
                if (this.ServerId < 0)
                {
                    await this.RunClient(runtime, clusterManager, subscriptionName);
                }
                else
                {
                    await this.RunServer(runtime, clusterManager, subscriptionName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} :: ex: {ex.ToString()}");
            }
        }

        private async Task RunClient(IActorRuntime runtime, ActorId clusterManager, string subscriptionName)
        {
            CancellationTokenSource cancelSource = new CancellationTokenSource();
            StartRaftServers(this.ConnectionString, this.TopicName, this.ClusterSize);

            var receiver = new AzureMessageReceiver(runtime, this.ConnectionString, this.TopicName, this.LocalId, subscriptionName);
            var nowait = receiver.RunAsync(cancelSource.Token);
            receiver.ResponseReceived += (s, e) =>
            {
                this.completed.SetResult(e);
            };

            // Now send the requested number of ClientRequestEvents to the cluster, and wait for each response.
            for (int i = 0; i < this.NumRequests; i++)
            {
                string command = $"request-{i}";
                Console.WriteLine($"<Client> sending {command}.");
                this.completed = new TaskCompletionSource<ClientResponseEvent>();
                runtime.SendEvent(clusterManager, new ClientRequestEvent(command));
                var response = await this.completed.Task;
                Console.WriteLine($"<Client> received response for {response.Command} from  {response.Server}.");
            }
        }

        private async Task RunServer(IActorRuntime runtime, ActorId clusterManager, string subscriptionName)
        {
            if (this.Debug)
            {
                Console.WriteLine("Attach debugger");
                await Task.Delay(60000);
            }

            CancellationTokenSource cancelSource = new CancellationTokenSource();

            if (this.ClientProcessId == 0)
            {
                throw new Exception("Server should have a client process id");
            }

            MonitorClientProcess(this.ClientProcessId);

            // We create a server host that will create and wrap a Raft server instance (implemented
            // as a Coyote state machine), and execute it using the Coyote runtime.
            var host = new AzureServer(runtime, this.ConnectionString, this.TopicName, this.ServerId, this.ClusterSize, clusterManager);
            this.LocalId = host.HostedServer;
            host.Initialize();
            host.Start();

            var receiver = new AzureMessageReceiver(runtime, this.ConnectionString, this.TopicName, this.LocalId, subscriptionName);
            await receiver.RunAsync(cancelSource.Token);
        }

        /// <summary>
        /// Callback that is invoked when an unhandled exception is thrown in the Coyote runtime.
        /// </summary>
        private static void RuntimeOnFailure(Exception ex)
        {
            int processId = Process.GetCurrentProcess().Id;
            Console.WriteLine($"Server process with id {processId} failed with exception:");
            Console.WriteLine(ex);
            Environment.Exit(1);
        }

        #region infrastructure code
        private static void StartRaftServers(string connectionString, string topicName, int size)
        {
            int processId = Process.GetCurrentProcess().Id;
            var serverPath = Assembly.GetExecutingAssembly().Location;

            for (int idx = 0; idx < size; idx++)
            {
                int serverId = idx;
                Process process = new Process();
                bool debugProcess = idx == 0;
                string debugOption = debugProcess ? " --debug" : string.Empty;
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = $" {serverPath} --connection-string \"{connectionString}\" " +
                    $"--topic-name \"{topicName}\" --local-cluster-size {size} --server-id {serverId} " +
                    $"--client-process-id {processId}" + debugOption;

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.OutputDataReceived += (sender, args) => Console.WriteLine($"Server-{serverId}: {args.Data}");

                process.Start();
                process.BeginOutputReadLine();

                Console.WriteLine($"<Client> started server process with id {process.Id}.");

                ServerProcesses.Add(process);
            }

            AppDomain.CurrentDomain.DomainUnload += KillServerProcesses;
            AppDomain.CurrentDomain.ProcessExit += KillServerProcesses;
            AppDomain.CurrentDomain.UnhandledException += KillServerProcesses;
            Console.CancelKeyPress += KillServerProcesses;
        }

        private static void KillServerProcesses(object sender, EventArgs e)
        {
            foreach (var process in ServerProcesses)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();

                    Console.WriteLine($"<Client> killed server process with id {process.Id}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now} :: Exception: {ex.Message}");
                }
            }
        }

        private static void MonitorClientProcess(int clientProcessId)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        Process.GetProcessById(clientProcessId);
                        await Task.Delay(2000);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentException argEx)
                    {
                        Console.WriteLine($"Client process with id {clientProcessId} " +
                            "is not running. Terminating server ...");
                        Environment.Exit(0);
                    }

                    Console.WriteLine($"{DateTime.Now} :: ex: {ex.Message}");
                }
            });
        }
        #endregion
    }
}
