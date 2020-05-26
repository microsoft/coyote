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

namespace Coyote.Telemetry
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

        public CoyoteTelemetryClient(Configuration configuration)
        {
#if NET47
            this.Framework = "net47";
#elif NET48
            this.Framework = "net48";
#elif NETSTANDARD2_0
            this.Framework = "netstandard2.0";
#elif NETSTANDARD2_1
            this.Framework = "netstandard2.1";
#elif NETCOREAPP3_1
            this.Framework = "netcoreapp3.1";
#endif
            this.Enabled = configuration.EnableTelemetry;
            if (this.Enabled)
            {
                this.Name = $"coyote{Process.GetCurrentProcess().Id.ToString()}";
                _ = this.ConnectToServer();
            }
        }

        ~CoyoteTelemetryClient()
        {
            this.Dispose(false);
        }

        public async Task TrackEventAsync(string name)
        {
            if (!this.Enabled)
            {
                return;
            }

            var e = new TelemetryEvent(this.Framework, name, this.Name);
            if (this.Server != null)
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
            if (this.Server != null)
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
        /// Opens the remote notification listener. If this is
        /// not a parallel testing process, then this operation
        /// does nothing.
        /// </summary>
        private async Task ConnectToServer()
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
                    StartServer();
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

        private static void StartServer()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
#if NETFRAMEWORK
            ProcessStartInfo startInfo = new ProcessStartInfo(assembly, "telemetry server");
#else
            ProcessStartInfo startInfo = new ProcessStartInfo("dotnet", assembly + " telemetry server");
#endif
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = startInfo;
            if (!process.Start())
            {
                Console.WriteLine("Error starting coyote telemetry");
            }
        }

        private void Close()
        {
            this.PendingCleared.WaitOne(5000);
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

        internal static string GetOrCreateMachineId(out bool firstTime)
        {
            firstTime = false;
            string path = CoyoteHomePath;
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

                        return id.ToString();
                    }
                    catch
                    {
                        // race condition, another process beat us to it?
                        return File.ReadAllText(fullpath);
                    }
                }
                else
                {
                    return File.ReadAllText(fullpath);
                }
            }
            catch
            {
                // ignore race conditions on this first time file.
            }

            return null;
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
