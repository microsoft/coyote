// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using AppInsightsClient = Microsoft.ApplicationInsights.TelemetryClient;

namespace Microsoft.Coyote.Telemetry
{
    /// <summary>
    /// Thread-safe client for sending telemetry messages to Azure.
    /// </summary>
    /// <remarks>
    /// See <see href="https://github.com/microsoft/ApplicationInsights-dotnet"/>.
    /// </remarks>
    internal class TelemetryClient
    {
        /// <summary>
        /// Path to the Coyote home directory where the UUID is stored.
        /// </summary>
        private static string CoyoteHomePath => IsWindowsLike ?
            Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "Microsoft", "coyote") :
            Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".microsoft", "coyote");

        /// <summary>
        /// File name where the UUID is stored.
        /// </summary>
        private const string IdFileName = "device_id.txt";

        /// <summary>
        /// Used to synchronize access to the telemetry client.
        /// </summary>
        private static readonly object SyncObject = new object();

        /// <summary>
        /// The current instance of the telemetry client.
        /// </summary>
        private static TelemetryClient Current;

        /// <summary>
        /// The App Insights client.
        /// </summary>
        private readonly AppInsightsClient Client;

        /// <summary>
        /// True if telemetry is enabled, else false.
        /// </summary>
        private readonly bool IsEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClient"/> class.
        /// </summary>
        private TelemetryClient(bool isEnabled)
        {
            if (isEnabled)
            {
                TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
                configuration.InstrumentationKey = "17a6badb-bf2d-4f5d-959b-6843b8bb1f7f";
                this.Client = new AppInsightsClient(configuration);

                string version = typeof(Runtime.CoyoteRuntime).Assembly.GetName().Version.ToString();
                this.Client.Context.GlobalProperties["coyote"] = version;
#if NETFRAMEWORK
                this.Client.Context.GlobalProperties["dotnet"] = ".NET Framework";
#else
                this.Client.Context.GlobalProperties["dotnet"] = RuntimeInformation.FrameworkDescription;
#endif
                this.Client.Context.Device.Id = GetOrCreateDeviceId(out bool isFirstTime);
                this.Client.Context.Device.OperatingSystem = Environment.OSVersion.Platform.ToString();
                this.Client.Context.Session.Id = Guid.NewGuid().ToString();

                if (isFirstTime)
                {
                    this.TrackEvent("welcome");
                }
            }

            this.IsEnabled = isEnabled;
        }

        /// <summary>
        /// Returns the existing telemetry client if one has already been created for this process,
        /// or creates and returns a new one with the specified configuration.
        /// </summary>
        internal static TelemetryClient GetOrCreate(Configuration configuration)
        {
            lock (SyncObject)
            {
                Current ??= new TelemetryClient(configuration.IsTelemetryEnabled);
                return Current;
            }
        }

        /// <summary>
        /// Tracks the specified telemetry event.
        /// </summary>
        internal void TrackEvent(string name)
        {
            if (this.IsEnabled)
            {
                lock (SyncObject)
                {
                    try
                    {
                        IO.Debug.WriteLine("[coyote::telemetry] Tracking event: {0}.", name);
                        this.Client.TrackEvent(new EventTelemetry(name));
                    }
                    catch (Exception ex)
                    {
                        IO.Debug.WriteLine("[coyote::telemetry] Error sending event: {0}", ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Tracks the specified telemetry metric.
        /// </summary>
        internal void TrackMetric(string name, double value)
        {
            if (this.IsEnabled)
            {
                lock (SyncObject)
                {
                    try
                    {
                        IO.Debug.WriteLine("[coyote::telemetry] Tracking metric: {0}={1}.", name, value);
                        this.Client.TrackMetric(new MetricTelemetry(name, value));
                    }
                    catch (Exception ex)
                    {
                        IO.Debug.WriteLine("[coyote::telemetry] Error sending metric: {0}", ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Flushes any buffered in-memory telemetry data.
        /// </summary>
        internal void Flush()
        {
            if (this.IsEnabled)
            {
                lock (SyncObject)
                {
                    try
                    {
                        this.Client.Flush();
                    }
                    catch (Exception ex)
                    {
                        IO.Debug.WriteLine("[coyote::telemetry] Error flushing: {0}", ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the unique device id or creates a new one.
        /// </summary>
        private static string GetOrCreateDeviceId(out bool isFirstTime)
        {
            string deviceId = Guid.NewGuid().ToString();
            isFirstTime = true;

            int attempts = 5;
            while (attempts-- > 0)
            {
                try
                {
                    string fullpath = Path.Combine(CoyoteHomePath, IdFileName);
                    if (!File.Exists(fullpath))
                    {
                        Directory.CreateDirectory(CoyoteHomePath);
                        using StreamWriter writer = new StreamWriter(fullpath);
                        writer.Write(deviceId);
                    }
                    else
                    {
                        deviceId = File.ReadAllText(fullpath);
                        isFirstTime = false;
                    }

                    break;
                }
                catch
                {
                    Thread.Sleep(1);
                }
            }

            return deviceId;
        }

        /// <summary>
        /// Returns true if this is a Windows platform, else false.
        /// </summary>
        private static bool IsWindowsLike => Environment.OSVersion.Platform == PlatformID.Win32NT ||
            Environment.OSVersion.Platform == PlatformID.Win32S ||
            Environment.OSVersion.Platform == PlatformID.Win32Windows ||
            Environment.OSVersion.Platform == PlatformID.WinCE ||
            Environment.OSVersion.Platform == PlatformID.Xbox;
    }
}
