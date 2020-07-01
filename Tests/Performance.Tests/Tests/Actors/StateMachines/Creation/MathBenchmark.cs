// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Coyote.Performance.Tests.Actors.StateMachines
{
    [MinColumn, MaxColumn, MeanColumn, Q1Column, Q3Column, RankColumn]
    [MarkdownExporter, HtmlExporter, CsvExporter, CsvMeasurementsExporter, RPlotExporter]
    public class MathBenchmark
    {
        private double Sum = 0;

        [Benchmark]
        public void SimpleMath()
        {
            this.Sum = 0;
            for (int i = 0; i < 1000000; i++)
            {
                this.Sum += Math.Sqrt(this.Sum);
            }
        }
    }
}
