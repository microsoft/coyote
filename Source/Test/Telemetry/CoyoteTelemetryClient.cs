// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Microsoft.Coyote.SmartSockets;

namespace Microsoft.Coyote.Telemetry
{
    internal class CoyoteTelemetryClient : IDisposable
    {
        private SmartSocketClient Server;
        private CancellationTokenSource FindServerTokenSource = new CancellationTokenSource();
        private bool Enabled;
        private List<SocketMessage> Pending = new List<SocketMessage>();
        private readonly ManualResetEvent PendingCleared = new ManualResetEvent(false);
        private readonly string Name;
        private const string CoyoteMachineIdFileName = "CoyoteMachineId.txt";
        private readonly string Framework = "unknown";
        private static string MachineId;
        private static CoyoteTelemetryServer InProcServer;

        public CoyoteTelemetryClient(Configuration configuration)
        {
            this.Enabled = configuration.EnableTelemetry && !configuration.RunAsParallelBugFindingTask;
            if (string.IsNullOrEmpty(configuration.DotnetFrameworkVersion))
            {
#if NETSTANDARD2_1
                this.Framework = "netstandard2.1";
#elif NETSTANDARD2_0
                this.Framework = "netstandard2.0";
#elif NETSTANDARD
                this.Framework = "netstandard";
#elif NETCOREAPP3_1
                this.Framework = "netcoreapp3.1";
#elif NETCOREAPP
                this.Framework = "netcoreapp";
#elif NET48
                this.Framework = "net48";
#elif NET47
                this.Framework = "net47";
#elif NETFRAMEWORK
                this.Framework = "net";
#endif
            }
            else
            {
                this.Framework = configuration.DotnetFrameworkVersion;
            }

            if (this.Enabled)
            {
                if (string.IsNullOrEmpty(configuration.TelemetryServerPath))
                {
                    // then server is running in-proc (as we do for unit testing)
                    if (InProcServer == null)
                    {
                        InProcServer = new CoyoteTelemetryServer(false);
                    }

                    return;
                }
                else
                {
                    // run the server out of proc.
                    this.Name = $"coyote{Process.GetCurrentProcess().Id}";
                    _ = this.ConnectToServer(configuration.TelemetryServerPath);
                }
            }
        }

        ~CoyoteTelemetryClient()
        {
            this.Dispose(false);
        }

        public static void PrintTelemetryMessage(TextWriter writer)
        {
            writer.WriteLine();
            writer.WriteLine("Telemetry is enabled");
            writer.WriteLine("--------------------");
            writer.WriteLine("Microsoft Coyote tools collect usage data in order to help us improve your experience. " +
                "The data is anonymous. It is collected by Microsoft and shared with the community. " +
                "You can opt-out of telemetry by setting the COYOTE_CLI_TELEMETRY_OPTOUT environment variable to '1' or 'true'.");
            writer.WriteLine();
            writer.WriteLine("Read more about Microsoft Coyote Telemetry at http://aka.ms/coyote-telemetry");
            writer.WriteLine("--------------------------------------------------------------------------------------------");
        }

        public async Task TrackEventAsync(string name)
        {
            if (!this.Enabled)
            {
                return;
            }

            var e = new TelemetryEvent(this.Framework, name, this.Name);

            if (InProcServer != null)
            {
                InProcServer.HandleEvent(e);
            }
            else if (this.Server != null)
            {
                await this.Server.SendReceiveAsync(e);
            }
            else
            {
                // queue the event until the telemetry server is ready.
                lock (this.Pending)
                {
                    this.Pending.Add(e);
                }
            }
        }

        public async Task TrackMetricAsync(string name, double value)
        {
            if (!this.Enabled)
            {
                return;
            }

            var e = new TelemetryMetric(this.Framework, name, this.Name, value);

            if (InProcServer != null)
            {
                InProcServer.HandleMetric(e);
            }
            else if (this.Server != null)
            {
                await this.Server.SendReceiveAsync(e);
            }
            else
            {
                // queue the event until the telemetry server is ready.
                lock (this.Pending)
                {
                    this.Pending.Add(e);
                }
            }
        }

        /// <summary>
        /// Starts a telemetry server in a separate coyote process.
        /// </summary>
        private async Task ConnectToServer(string serverPath)
        {
            this.FindServerTokenSource = new CancellationTokenSource();
            var token = this.FindServerTokenSource.Token;

            string serviceName = CoyoteTelemetryServer.TelemetryServerEndPoint;
            var resolver = new SmartSocketTypeResolver(typeof(TelemetryEvent), typeof(TelemetryMetric));

            SmartSocketClient client = null;
            var findTask = SmartSocketClient.FindServerAsync(serviceName, this.Name, resolver, token,
                CoyoteTelemetryServer.UdpGroupAddress, CoyoteTelemetryServer.UdpGroupPort);
            try
            {
                if (findTask.Wait(1000, token))
                {
                    client = findTask.Result;
                }
            }
            catch
            {
                // timeout or cancelled.
                if (token.IsCancellationRequested)
                {
                    return;
                }
            }

            if (client == null)
            {
                try
                {
                    StartServer(serverPath);
                    client = await SmartSocketClient.FindServerAsync(serviceName, this.Name, resolver, token,
                        CoyoteTelemetryServer.UdpGroupAddress, CoyoteTelemetryServer.UdpGroupPort);
                }
                catch
                {
                    // failed to connect to new telemetry server
                    this.Enabled = false;
                    return;
                }
            }

            client.Error += this.OnClientError;
            client.ServerName = serviceName;
            this.Server = client;

            // send any pending queue of stuff.
            List<SocketMessage> pending = null;
            lock (this.Pending)
            {
                pending = this.Pending;
                this.Pending = null;
            }

            foreach (var e in pending)
            {
                await this.Server.SendReceiveAsync(e);
            }

            this.PendingCleared.Set();

            _ = Task.Run(this.SendHeartbeats);
        }

        private void OnClientError(object sender, Exception e)
        {
            // todo: error handling?
        }

        private static void StartServer(string assembly)
        {
            string ext = Path.GetExtension(assembly);
            string program = assembly;
            string args = "telemetry server";
            if (string.Compare(ext, ".dll", StringComparison.OrdinalIgnoreCase) == 0)
            {
                args = "\"" + assembly + "\" telemetry server";
                program = "dotnet";
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(program, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process
            {
                StartInfo = startInfo
            };
            if (!process.Start())
            {
                Console.WriteLine("Error starting coyote telemetry");
            }
        }

        private void Close()
        {
            if (InProcServer != null)
            {
                InProcServer.Flush();
            }
            else if (this.Enabled)
            {
                this.PendingCleared.WaitOne(5000);
            }

            this.FindServerTokenSource.Cancel();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.Close();
        }

        private async void SendHeartbeats()
        {
            // This keeps the server alive until this test finishes (in case the test
            // takes longer than 60 seconds!).
            var token = this.FindServerTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(30000, token);
                    if (this.Server != null)
                    {
                        await this.Server.SendReceiveAsync(new SocketMessage("ping", this.Name));
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        internal static async Task<Tuple<string, bool>> GetOrCreateMachineId()
        {
            bool firstTime = false;
            if (MachineId != null)
            {
                return new Tuple<string, bool>(MachineId, firstTime);
            }

            string path = CoyoteHomePath;
            int retries = 3;
            while (retries-- > 0)
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    string fullpath = Path.Combine(path, CoyoteMachineIdFileName);
                    if (!File.Exists(fullpath))
                    {
                        try
                        {
                            firstTime = true;
                            Guid id = Guid.NewGuid();
                            using (StreamWriter writer = new StreamWriter(fullpath))
                            {
                                writer.Write(id.ToString());
                            }

                            MachineId = id.ToString();
                        }
                        catch
                        {
                            // race condition, another process beat us to it?
                            MachineId = File.ReadAllText(fullpath);
                        }
                    }
                    else
                    {
                        MachineId = File.ReadAllText(fullpath);
                    }
                }
                catch
                {
                    // ignore race conditions on this first time file.
                    await Task.Delay(50);
                }
            }

            if (MachineId == null)
            {
                // hmmm, something is horribly wrong with the file system, so just invent a new guid for now.
                MachineId = Guid.NewGuid().ToString();
            }

            return new Tuple<string, bool>(MachineId, firstTime);
        }

        internal static string CoyoteHomePath
        {
            get
            {
                return IsWindowsLike ? Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "Microsoft", "coyote") :
                    Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".microsoft", "coyote");
            }
        }

        internal static bool IsWindowsLike
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Win32NT ||
                    Environment.OSVersion.Platform == PlatformID.Win32S ||
                    Environment.OSVersion.Platform == PlatformID.Win32Windows ||
                    Environment.OSVersion.Platform == PlatformID.WinCE ||
                    Environment.OSVersion.Platform == PlatformID.Xbox;
            }
        }
    }
}
