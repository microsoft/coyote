// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Runtime;

// FN_TODO: Experiment by adding task-pct hacks for random strategy as well to schedule groups/chains of tasks.
namespace Microsoft.Coyote.Testing.Interleaving
{
    /// <summary>
    /// A simple (but effective) randomized scheduling strategy.
    /// </summary>
    internal class RandomStrategy : InterleavingStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RandomStrategy"/> class.
        /// </summary>
        internal RandomStrategy(Configuration configuration, IRandomValueGenerator generator, bool isFair = true)
            : base(configuration, generator, isFair)
        {
            this.registeredOps = new HashSet<ControlledOperation>();
        }

        private readonly HashSet<ControlledOperation> registeredOps;

        private int ContextSwitchNumber;

        internal void PrintTaskPCTStatsForIteration(uint iteration)
        {
            Console.WriteLine(string.Empty);
            Console.WriteLine($"===========<IMP_RandomStrategy> [PrintTaskPCTStatsForIteration] RANDOM STATS for ITERATION: {iteration}");
            Console.WriteLine($"                  TOTAL ASYNC OPS: {this.registeredOps.Count}");
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            if (iteration > 0)
            {
                // FN_TODO: print the stat for the last iteration also
                this.PrintTaskPCTStatsForIteration(iteration - 1);
            }

            // The random strategy just needs to reset the number of scheduled steps during
            // the current iretation.
            this.StepCount = 0;
            this.registeredOps.Clear();
            return true;
        }

        private void DebugPrintBeforeGetNextOperation(IEnumerable<ControlledOperation> opss)
        {
            this.ContextSwitchNumber += 1;
            var ops = opss.ToList();
            IO.Debug.WriteLine($"          ops.Count = {ops.Count}");
            int countt = 0;
            foreach (var op in ops)
            {
                if (countt == 0)
                {
                    IO.Debug.Write($"          {op}");
                }
                else
                {
                    IO.Debug.Write($", {op}");
                }

                countt++;
            }

            IO.Debug.WriteLine(string.Empty);

            countt = 0;
            foreach (var op in ops)
            {
                if (countt == 0)
                {
                    IO.Debug.Write($"          {op.Status}");
                }
                else
                {
                    IO.Debug.Write($", {op.Status}");
                }

                countt++;
            }

            IO.Debug.WriteLine(string.Empty);

            countt = 0;
            foreach (var op in ops)
            {
                if (countt == 0)
                {
                    IO.Debug.Write($"          {op.GetType()}");
                }
                else
                {
                    IO.Debug.Write($", {op.GetType()}");
                }

                countt++;
            }

            IO.Debug.WriteLine(string.Empty);

            HashSet<ControlledOperation> newConcurrentOps = new HashSet<ControlledOperation>();
            foreach (var op in ops)
            {
                if (!this.registeredOps.Contains(op))
                {
                    newConcurrentOps.Add(op);
                    this.registeredOps.Add(op);
                }
            }

            IO.Debug.WriteLine($"          # new operations added {newConcurrentOps.Count}");
            // Specification.Assert((newConcurrentOps.Count <= 1) || (newConcurrentOps.Count == 2 && this.ContextSwitchNumber == 1),
            //     $"     <TaskSummaryLog-ERROR> At most one new operation must be added across context switch.");
            if (!((newConcurrentOps.Count <= 1) || (newConcurrentOps.Count == 2 && this.ContextSwitchNumber == 1)))
            {
                Console.WriteLine($"     <TaskSummaryLog-ERROR> At most one new operation must be added across context switch.");
            }

            int cases = 0;

            if (newConcurrentOps.Count == 0)
            {
                Console.WriteLine($"     <TaskSummaryLog> T-case 1.): No new task added.");
                cases = 1;
            }

            foreach (var op in newConcurrentOps)
            {
                IO.Debug.WriteLine($"          newConcurrentOps: {op}, Spawner: {op.ParentTask}");
                if (op.IsContinuationTask)
                {
                    if (op.ParentTask == null)
                    {
                        Console.WriteLine($"     <TaskSummaryLog> T-case 3.): Continuation task {op} (id = {op.Id}) is the first task to be created!");
                    }
                    else
                    {
                        Console.WriteLine($"     <TaskSummaryLog> T-case 3.): Continuation task {op} (id = {op.Id}) created by {op.ParentTask} (id = {op.ParentTask.Id}).");
                    }

                    cases = 3;
                }
                else
                {
                    if (op.ParentTask == null)
                    {
                        Console.WriteLine($"     <TaskSummaryLog> T-case 2.): Spawn task {op} (id = {op.Id}) is the first task to be created!");
                    }
                    else
                    {
                        Console.WriteLine($"     <TaskSummaryLog> T-case 2.): Spawn task {op} (id = {op.Id}) created by {op.ParentTask} (id = {op.ParentTask.Id}).");
                    }

                    cases = 2;
                }
            }

            // Specification.Assert( (cases == 1) || (cases == 2) || (cases == 3),
            //     $"     <TaskSummaryLog-ERROR> At most one new operation must be added across context switch.");
            if (!((cases == 1) || (cases == 2) || (cases == 3)))
            {
                Console.WriteLine($"     <TaskSummaryLog-ERROR> At most one new operation must be added across context switch.");
            }

            // IO.Debug.WriteLine(string.Empty);
        }

        private static void DebugPrintAfterGetNextOperation(ControlledOperation next)
        {
            IO.Debug.WriteLine($"          next = {next}");
            Console.WriteLine($"     <TaskSummaryLog> Scheduled: {next}");
            // IO.Debug.WriteLine();
            // IO.Debug.WriteLine();
            // IO.Debug.WriteLine();
            // IO.Debug.WriteLine();
            // IO.Debug.WriteLine();
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            foreach (var op in ops)
            {
                this.registeredOps.Add(op);
            }

            int idx = this.RandomValueGenerator.Next(ops.Count());
            next = ops.ElementAt(idx);
            this.StepCount++;
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(ControlledOperation current, int maxValue, out bool next)
        {
            next = false;
            if (this.RandomValueGenerator.Next(maxValue) is 0)
            {
                next = true;
            }

            this.StepCount++;
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextIntegerChoice(ControlledOperation current, int maxValue, out int next)
        {
            next = this.RandomValueGenerator.Next(maxValue);
            this.StepCount++;
            return true;
        }

        /// <inheritdoc/>
        internal override int GetStepCount() => this.StepCount;

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached()
        {
            if (this.MaxSteps is 0)
            {
                return false;
            }

            return this.StepCount >= this.MaxSteps;
        }

        /// <inheritdoc/>
        internal override string GetDescription() => $"random[seed:{this.RandomValueGenerator.Seed}]";

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.StepCount = 0;
        }
    }
}
