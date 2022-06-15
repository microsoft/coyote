// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace Microsoft.Coyote.Benchmarking
{
    /// <summary>
    /// The BenchmarkDotNet MemoryDiagnoser does too much, costing about 20% perf hit.
    /// This is a cheaper version that simply captures GC TotalMemory.
    /// </summary>
    internal class TotalMemoryDiagnoser : IDiagnoser
    {
        public long TotalMemory;

        public IEnumerable<string> Ids => new[] { nameof(TotalMemoryDiagnoser) };

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
                case HostSignal.AfterActualRun:
                    this.TotalMemory = GC.GetTotalMemory(true);
                    break;
                default:
                    break;
            }
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            yield return new Metric(TotalMemoryDescriptor.Instance, this.TotalMemory);
        }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) => Array.Empty<ValidationError>();

        private class TotalMemoryDescriptor : IMetricDescriptor
        {
            internal static readonly IMetricDescriptor Instance = new TotalMemoryDescriptor();

            public string Id => "TotalMemory";
            public string DisplayName => "TotalMemory";
            public string Legend => "Total GC memory";
            public string NumberFormat => "N";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => SizeUnit.B.Name;
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
        }
    }
}
