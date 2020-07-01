// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Coyote.Performance.Tests.Actors.StateMachines
{
    [MinColumn, MaxColumn, MeanColumn, Q1Column, Q3Column, RankColumn]
    [MarkdownExporter, HtmlExporter, CsvExporter, CsvMeasurementsExporter, RPlotExporter]
    public class MemoryBenchmark
    {
        private List<double> Data;

        [Benchmark]
        public void MemoryTest()
        {
            double result = 0;
            this.Data = new List<double>();
            for (int i = 0; i < 100000; i++)
            {
                result += Math.Sqrt(result);
                this.Data.Add(result);
            }
        }
    }
}
