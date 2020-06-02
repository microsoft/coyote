// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Coyote.SmartSockets;

namespace Microsoft.Coyote.Telemetry
{
    /// <summary>
    /// A custom SocketMessage used to convey a telemetry event.
    /// </summary>
    [DataContract]
    internal class TelemetryEvent : SocketMessage
    {
        [DataMember]
        public string Framework { get; set; }

        public TelemetryEvent(string framework, string id, string sender)
            : base(id, sender)
        {
            this.Framework = framework;
        }
    }

    /// <summary>
    /// A custom SocketMessage used to convey a telemetry metric.
    /// </summary>
    [DataContract]
    internal class TelemetryMetric : SocketMessage
    {
        [DataMember]
        public string Framework { get; set; }

        [DataMember]
        public double Value { get; set; }

        public TelemetryMetric(string framework, string id, string sender, double value)
            : base(id, sender)
        {
            this.Value = value;
            this.Framework = framework;
        }
    }

    /// <summary>
    /// This is a SmartSocketServer designed for sending the coyote custom telemetry messages
    /// to Azure.  The server runs in a separate process communicating for smart sockets.
    /// It sticks around for 60 seconds then terminates, unless a heartbeat or telemetry
    /// message is received from another coyote process.  You can debug this server easily
    /// by running "coyote telemetry server" from the command line.
    /// See <see href="https://github.com/microsoft/ApplicationInsights-dotnet"/>.
    /// </summary>
    internal class CoyoteTelemetryServer
    {
        /// <summary>
        /// The server socket that all the coyote apps will connect to.
        /// </summary>
        private SmartSocketServer Server;

        /// <summary>
        /// The App Insights client.
        /// </summary>
        private TelemetryClient Telemetry;

        public const string TelemetryServerEndPoint = "CoyoteTelemetryServer.132d4357-1b32-473f-994b-e35eccaacd46";

        private string MachineId;
        private DateTime LastEvent;
        private bool PendingEvents;
        private readonly bool Verbose;

        private readonly TimeSpan ServerTerminateDelay = TimeSpan.FromSeconds(30);

        internal const string UdpGroupAddress = "226.10.10.3";
        internal const int UdpGroupPort = 37993;

        public CoyoteTelemetryServer(bool verbose)
        {
            this.Verbose = verbose;

            // you may use different options to create configuration as shown later in this article
            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = "17a6badb-bf2d-4f5d-959b-6843b8bb1f7f";
            this.Telemetry = new TelemetryClient(configuration);
            string version = typeof(Microsoft.Coyote.Runtime.CoyoteRuntime).Assembly.GetName().Version.ToString();
            this.Telemetry.Context.GlobalProperties["coyote"] = version;
            this.Telemetry.Context.Device.OperatingSystem = Environment.OSVersion.Platform.ToString();
            this.Telemetry.Context.Session.Id = Guid.NewGuid().ToString();
            this.LastEvent = DateTime.Now;
        }

        /// <summary>
        /// Opens the local server for local coyote test processes to connect to.
        /// </summary>
        internal async Task RunServerAsync()
        {
            this.WriteLine("Starting telemetry server...");

            var result = await CoyoteTelemetryClient.GetOrCreateMachineId();
            this.MachineId = result.Item1;
            this.Telemetry.Context.Device.Id = this.MachineId;

            var resolver = new SmartSocketTypeResolver(typeof(TelemetryEvent), typeof(TelemetryMetric));
            var server = SmartSocketServer.StartServer(TelemetryServerEndPoint, resolver, null /* localhost only */, UdpGroupAddress, UdpGroupPort);
            server.ClientConnected += this.OnClientConnected;
            server.ClientDisconnected += this.OnClientDisconnected;
            this.Server = server;

            // Here we see the reason for this entire class.  In order to allow coyote.exe to terminate quickly
            // and not lose telemetry messages, we have to wait a bit to allow the App Insights cloud messages
            // to get through, then we can safely terminate this server process.
            while (this.Telemetry != null && this.LastEvent + this.ServerTerminateDelay > DateTime.Now)
            {
                await Task.Delay((int)this.ServerTerminateDelay.TotalMilliseconds);
                if (this.PendingEvents)
                {
                    this.WriteLine("Flushing telemetry...");
                    this.Flush();
                    this.LastEvent = DateTime.Now; // go around again to give flush time to finish.
                }
            }

            this.Telemetry = null;
        }

        /// <summary>
        /// Flush events to Azure.
        /// </summary>
        internal void Flush()
        {
            if (this.Telemetry != null)
            {
                this.PendingEvents = false;
                this.Telemetry.Flush();
            }
        }

        /// <summary>
        /// Called when a separate coyote test/replay process termiantes.
        /// </summary>
        private void OnClientDisconnected(object sender, SmartSocketClient e)
        {
            this.WriteLine("Client disconnected: " + e.Name);
        }

        /// <summary>
        /// Called when a separate coyote test/replay process starts up and connects to
        /// this server.
        /// </summary>
        private void OnClientConnected(object sender, SmartSocketClient e)
        {
            // A coyote process has started up, so this socket will be used to receive telemetry requests.
            Task.Run(() => this.HandleClient(e));
        }

        private async void HandleClient(SmartSocketClient e)
        {
            this.WriteLine("Client connected: " + e.Name);
            this.LastEvent = DateTime.Now;

            while (e.IsConnected && this.Telemetry != null)
            {
                try
                {
                    var msg = await e.ReceiveAsync();
                    if (msg != null)
                    {
                        this.LastEvent = DateTime.Now;
                        if (msg is TelemetryEvent tm)
                        {
                            this.HandleEvent(tm);
                        }
                        else if (msg is TelemetryMetric metric)
                        {
                            this.HandleMetric(metric);
                        }
                        else
                        {
                            this.WriteLine("Received heartbeat");
                        }

                        await e.SendAsync(new SocketMessage("ok", TelemetryServerEndPoint));
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Calls the App Insights TrackEvent method.
        /// </summary>
        internal void HandleEvent(TelemetryEvent e)
        {
            try
            {
                this.WriteLine("Tracking event ({1}): {0}", e.Id, e.Framework);
                if (this.Telemetry != null)
                {
                    this.PendingEvents = true;
                    this.Telemetry.Context.GlobalProperties["dotnet"] = e.Framework;
                    this.Telemetry.TrackEvent(new EventTelemetry(e.Id));
                }
            }
            catch (Exception ex)
            {
                this.WriteLine("Error sending TrackEvent: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Calls the App Insights TrackMetric method.
        /// </summary>
        internal void HandleMetric(TelemetryMetric e)
        {
            try
            {
                this.WriteLine("Tracking metric ({2}): {0}={1}", e.Id, e.Value, e.Framework);
                if (this.Telemetry != null)
                {
                    this.PendingEvents = true;
                    this.Telemetry.Context.GlobalProperties["dotnet"] = e.Framework;
                    this.Telemetry.TrackMetric(new MetricTelemetry(e.Id, e.Value));
                }
            }
            catch (Exception ex)
            {
                this.WriteLine("Error sending TrackMetric: {0}", ex.Message);
            }
        }

        private void WriteLine(string msg, params object[] args)
        {
            if (this.Verbose)
            {
                Console.WriteLine(msg, args);
            }
        }
    }
}
