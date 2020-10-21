// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Microsoft.Coyote.Benchmarking
{
    /// <summary>
    /// Wrapper on the Cosmos DB table used to store the perf results.
    /// </summary>
    public class Storage
    {
        private const string CosmosDatabaseId = "actorperfdb";
        private const string SummaryTableName = "actorperfsummary";
        private const string CommitLogTableName = "commitlog";
        // private const string DetailsTableName = "actorperfdetails";

        // Maximum number of operations in a transactional batch is 100
        // See https://docs.microsoft.com/en-us/azure/cosmos-db/concepts-limits#per-request-limits
        // private const int BatchMaximum = 100;

        private CosmosClient CosmosClient;
        private Database CosmosDatabase;
        private Container SummaryContainer;

        private async Task Connect()
        {
            var endpointUri = Environment.GetEnvironmentVariable("AZURE_COSMOSDB_ENDPOINT");
            if (string.IsNullOrEmpty(endpointUri))
            {
                Console.WriteLine("AZURE_COSMOSDB_ENDPOINT is not set");
                return;
            }

            var primaryKey = Environment.GetEnvironmentVariable("AZURE_STORAGE_PRIMARY_KEY");
            if (string.IsNullOrEmpty(primaryKey))
            {
                Console.WriteLine("AZURE_STORAGE_PRIMARY_KEY is not set");
                return;
            }

            this.CosmosClient = new CosmosClient(endpointUri, primaryKey);
            var response = await this.CosmosClient.CreateDatabaseIfNotExistsAsync(CosmosDatabaseId);
            this.CosmosDatabase = response.Database;
        }

        internal async Task<int> UploadAsync(List<PerfSummary> summaries)
        {
            if (this.CosmosDatabase == null)
            {
                await this.Connect();
            }

            if (this.CosmosDatabase == null)
            {
                return 0;
            }

            if (this.SummaryContainer == null)
            {
                var response = await this.CosmosDatabase.CreateContainerIfNotExistsAsync(SummaryTableName, "/PartitionKey");
                this.SummaryContainer = response.Container;
            }

            int count = 0;

            foreach (var s in summaries)
            {
                bool better = true;
                try
                {
                    var response = await this.SummaryContainer.ReadItemAsync<PerfSummary>(s.Id, new PartitionKey(s.PartitionKey));
                    PerfSummary old = response.Resource;
                    if (old.TimeMean < s.TimeMean)
                    {
                        better = false;
                    }
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                }

                if (better)
                {
                    Console.WriteLine("===> Uploading summary for {0}...", s.TestName);
                    await this.SummaryContainer.UpsertItemAsync(s, new PartitionKey(s.PartitionKey));
                    count++;
                }
                else
                {
                    Console.WriteLine("===> Existing record is better for {0}...", s.TestName);
                }
            }

            return count;
        }

        internal async Task<List<PerfSummary>> DownloadAsync(string partitionKey, List<string> rowKeys)
        {
            List<PerfSummary> results = new List<PerfSummary>();

            if (this.CosmosDatabase == null)
            {
                await this.Connect();
            }

            if (this.CosmosDatabase == null)
            {
                return results;
            }

            if (this.SummaryContainer == null)
            {
                var response = await this.CosmosDatabase.CreateContainerIfNotExistsAsync(SummaryTableName, "/PartitionKey");
                this.SummaryContainer = response.Container;
            }

            foreach (var rowKey in rowKeys)
            {
                try
                {
                    var response = await this.SummaryContainer.ReadItemAsync<PerfSummary>(rowKey, new PartitionKey(partitionKey));
                    results.Add(response.Resource);
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                }
            }

            return results;
        }

        internal async Task UploadLogAsync(List<CommitHistoryEntity> log)
        {
            if (this.CosmosDatabase == null)
            {
                await this.Connect();
            }

            if (this.CosmosDatabase == null)
            {
                return;
            }

            var response = await this.CosmosDatabase.CreateContainerIfNotExistsAsync(CommitLogTableName, "/PartitionKey");
            var container = response.Container;

            foreach (var item in log)
            {
                Console.WriteLine("===> Uploading commit info {0}...", item.Id);
                await container.UpsertItemAsync(item, new PartitionKey(item.PartitionKey));
            }
        }
    }

    /// <summary>
    /// Entity representing a test result.
    /// </summary>
    public class PerfEntity
    {
        /// <summary>
        /// The row id.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// The parition key for the data.
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// The computer name where the test was run.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// The .net runtime version used.
        /// </summary>
        public string RuntimeVersion { get; set; }

        /// <summary>
        /// The git commit id of code being tested.
        /// </summary>
        public string CommitId { get; set; }

        /// <summary>
        /// UTC date and time the test was run.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The unique name of the test.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// The test iteration.
        /// </summary>
        public int Iteration { get; set; }

        /// <summary>
        /// Time to complete the test in milliseconds.
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// Standard deviation in the times.
        /// </summary>
        public double TimeStdDev { get; set; }

        /// <summary>
        /// Process working set during the test.
        /// </summary>
        public double Memory { get; set; }

        /// <summary>
        /// Standard deviation in the memory numbers.
        /// </summary>
        public double MemoryStdDev { get; set; }

        /// <summary>
        /// Process total CPU usage during the test as a % of total number of cores.
        /// </summary>
        public double Cpu { get; set; }

        /// <summary>
        /// Standard deviation in the CPU numbers.
        /// </summary>
        public double CpuStdDev { get; set; }

        /// <summary>
        /// Additional notes about the test.
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerfEntity"/> class.
        /// </summary>
        public PerfEntity(string machine, string runtime, string commit, string testName, int iteration)
        {
            this.MachineName = machine;
            this.RuntimeVersion = runtime;
            this.CommitId = commit;
            this.TestName = testName;
            this.Iteration = iteration;
            this.Date = DateTime.Now.ToUniversalTime();
            this.PartitionKey = string.Format("{0}.{1}", machine, runtime);
            this.Id = string.Format("{0}.{1}.{2}", commit, testName, iteration);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerfEntity"/> class.
        /// Needed for retreival.
        /// </summary>
        public PerfEntity()
        {
        }

        internal static void WriteHeaders(TextWriter outFile)
        {
            outFile.WriteLine("Name,Iteration,MinTime,StdDevTime,Memory,StdDevMemory,Cpu,StdDevCpu");
        }

        internal void WriteCsv(TextWriter outFile)
        {
            outFile.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", this.TestName, this.Iteration, this.Time, this.TimeStdDev, this.Memory, this.MemoryStdDev, this.Cpu, this.CpuStdDev);
        }
    }

    /// <summary>
    /// An entity representing the summary of all test iterations on a given test.
    /// </summary>
    public class PerfSummary
    {
        /// <summary>
        /// The row id.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// The parition key for the data.
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// The computer name where the test was run.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// The .net runtime version used.
        /// </summary>
        public string RuntimeVersion { get; set; }

        /// <summary>
        /// The git commit id of code being tested.
        /// </summary>
        public string CommitId { get; set; }

        /// <summary>
        /// UTC date and time the test was run.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The unique name of the test.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// The mean time in milliseconds.
        /// </summary>
        public double TimeMean { get; set; }

        /// <summary>
        /// The standard deviation of the test times.
        /// </summary>
        public double TimeStdDev { get; set; }

        /// <summary>
        /// The slope of the linear regression of the test times.
        /// </summary>
        public double TimeSlope { get; set; }

        /// <summary>
        /// The mean time in milliseconds.
        /// </summary>
        public double MemoryMean { get; set; }

        /// <summary>
        /// The standard deviation of the memory usage.
        /// </summary>
        public double MemoryStdDev { get; set; }

        /// <summary>
        /// The slope of the linear regression of the memory usage.
        /// </summary>
        public double MemorySlope { get; set; }

        /// <summary>
        /// The process cpu utilization as a percentage of total cores.
        /// </summary>
        public double CpuMean { get; set; }

        /// <summary>
        /// The standard deviation of the cpu times.
        /// </summary>
        public double CpuStdDev { get; set; }

        /// <summary>
        /// The slope of the linear regression of the cpu utilization.
        /// </summary>
        public double CpuSlope { get; set; }

        /// <summary>
        /// Additional notes about the test.
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// The raw test iterations.
        /// </summary>
        [IgnoreDataMember]
        internal readonly List<PerfEntity> Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerfSummary"/> class.
        /// </summary>
        public PerfSummary(List<PerfEntity> data)
        {
            PerfEntity e = data[0];
            this.MachineName = e.MachineName;
            this.RuntimeVersion = e.RuntimeVersion;
            this.CommitId = e.CommitId;
            this.Data = data;
            this.TestName = e.TestName;
            this.Date = DateTime.Now.ToUniversalTime();
            this.PartitionKey = string.Format("{0}.{1}", e.MachineName, e.RuntimeVersion);
            this.Id = string.Format("{0}.{1}", e.CommitId, e.TestName);

            // summaryize the data.
            double meanTime = MathHelpers.Mean(from i in data select i.Time);
            double meanMemory = MathHelpers.Mean(from i in data select i.Memory);
            double meanCpu = MathHelpers.Mean(from i in data select i.Cpu);

            double meanStdDevTime = MathHelpers.Mean(from i in data select i.TimeStdDev);
            double meanStdDevMemory = MathHelpers.Mean(from i in data select i.MemoryStdDev);
            double meanStdDevCpu = MathHelpers.Mean(from i in data select i.CpuStdDev);

            if (meanStdDevTime == 0)
            {
                meanStdDevTime = MathHelpers.StandardDeviation(from i in data select i.Time);
                meanStdDevMemory = MathHelpers.StandardDeviation(from i in data select i.Memory);
                meanStdDevCpu = MathHelpers.StandardDeviation(from i in data select i.Cpu);
            }

            double timeSlope = MathHelpers.LinearRegression(MathHelpers.ToDataPoints(from i in data select i.Time)).Slope / meanTime;
            double memSlope = MathHelpers.LinearRegression(MathHelpers.ToDataPoints(from i in data select i.Memory)).Slope / meanMemory;
            double cpuSlope = MathHelpers.LinearRegression(MathHelpers.ToDataPoints(from i in data select i.Cpu)).Slope / meanCpu;

            // more than 10% slope we have a problem!
            if (timeSlope > 0.1)
            {
                this.Comments = "Slow down?";
            }
            else if (memSlope > 0.1)
            {
                this.Comments = "Memory leak?";
            }
            else if (cpuSlope > 0.1)
            {
                this.Comments = "Thread leak?";
            }

            this.TimeMean = meanTime;
            this.TimeStdDev = meanStdDevTime;
            this.TimeSlope = timeSlope;
            this.MemoryMean = meanMemory;
            this.MemoryStdDev = meanStdDevMemory;
            this.MemorySlope = memSlope;
            this.CpuMean = meanCpu;
            this.CpuStdDev = meanStdDevCpu;
            this.CpuSlope = cpuSlope;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerfSummary"/> class.
        /// Needed for retreival.
        /// </summary>
        public PerfSummary()
        {
        }

        internal static void WriteHeaders(TextWriter outFile)
        {
            outFile.WriteLine("Machine,Runtime,Commit,Date,Test,TimeMean,TimeStdDev,TimeSlope,MemoryMean,MemoryStdDev,MemorySlope,CpuMean,CpuStdDev,CpuSlope");
        }

        internal void WriteCsv(TextWriter outFile)
        {
            outFile.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}", this.MachineName,
                this.RuntimeVersion, this.CommitId,  this.Date.ToLocalTime(), this.TestName, this.TimeMean,
                this.TimeStdDev, this.TimeSlope, this.MemoryMean, this.MemoryStdDev, this.MemorySlope,
                this.CpuMean, this.CpuStdDev, this.CpuSlope);
        }

        internal void SetPartitionKey(string partitionKey)
        {
            this.PartitionKey = partitionKey;
            int pos = partitionKey.IndexOf(".");
            if (pos > 0)
            {
                this.MachineName = partitionKey.Substring(0, pos);
                this.RuntimeVersion = partitionKey.Substring(pos + 1);
            }
        }
    }

    /// <summary>
    /// Entity representing a commit id.
    /// </summary>
    public class CommitHistoryEntity
    {
        /// <summary>
        /// The row id.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// The parition key for the data.
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// The id of the commit.
        /// </summary>
        public string CommitId { get; set; }

        /// <summary>
        /// The UTC commit date.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The author of the commit.
        /// </summary>
        public string Author { get; set; }
    }
}
