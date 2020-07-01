// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Perfolizer.Horology;

namespace Microsoft.Coyote.Benchmarking
{
    internal class CpuDiagnoser : IDiagnoser
    {
        private DateTime StartTime;
        private TimeSpan StartProcessorTime;
        private DateTime EndTime;
        private TimeSpan EndProcessorTime;

        public IEnumerable<string> Ids => new[] { nameof(CpuDiagnoser) };

        public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

        public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

        public void DisplayResults(ILogger logger)
        {
            // todo
        }

        public RunMode GetRunMode(BenchmarkCase benchmarkCase)
        {
            return RunMode.NoOverhead;
        }

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            switch (signal)
            {
                case HostSignal.BeforeActualRun:
                    this.StartTime = DateTime.Now;
                    this.StartProcessorTime = parameters.Process.TotalProcessorTime;
                    break;
                case HostSignal.AfterActualRun:
                    this.EndTime = DateTime.Now;
                    this.EndProcessorTime = parameters.Process.TotalProcessorTime;
                    break;
                default:
                    break;
            }
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            TimeSpan available = this.EndTime - this.StartTime;
            TimeSpan diff = this.EndProcessorTime - this.StartProcessorTime;
            double percent = diff.TotalSeconds / (Environment.ProcessorCount * available.TotalSeconds);
            yield return new Metric(CpuMetricDescriptor.Instance, percent);
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => Array.Empty<ValidationError>();

        private class CpuMetricDescriptor : IMetricDescriptor
        {
            internal static readonly IMetricDescriptor Instance = new CpuMetricDescriptor();

            public string Id => "CPU";
            public string DisplayName => "CPU%";
            public string Legend => "Total CPU Usage of the test process";
            public string NumberFormat => "#0.0000";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => SizeUnit.B.Name;
            public bool TheGreaterTheBetter => false;
        }
    }
}
