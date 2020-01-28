// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// Class representing an interactive scheduling strategy.
    /// </summary>
    internal sealed class InteractiveStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The installed logger.
        /// </summary>
        private readonly TextWriter Logger;

        /// <summary>
        /// The input cache.
        /// </summary>
        private readonly List<string> InputCache;

        /// <summary>
        /// The number of explored steps.
        /// </summary>
        private int ExploredSteps;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractiveStrategy"/> class.
        /// </summary>
        public InteractiveStrategy(Configuration configuration, TextWriter logger)
        {
            this.Logger = logger ?? new ConsoleLogger();
            this.Configuration = configuration;
            this.InputCache = new List<string>();
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            next = null;

            var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();

            if (enabledOperations.Count == 0)
            {
                this.Logger.WriteLine(">> No available machines to schedule ...");
                return false;
            }

            this.ExploredSteps++;

            var parsed = false;
            while (!parsed)
            {
                if (this.InputCache.Count >= this.ExploredSteps)
                {
                    var step = this.InputCache[this.ExploredSteps - 1];
                    int idx = 0;
                    if (step.Length > 0)
                    {
                        idx = Convert.ToInt32(step);
                    }
                    else
                    {
                        this.InputCache[this.ExploredSteps - 1] = "0";
                    }

                    next = enabledOperations[idx];
                    parsed = true;
                    break;
                }

                this.Logger.WriteLine(">> Available machines to schedule ...");
                for (int idx = 0; idx < enabledOperations.Count; idx++)
                {
                    var op = enabledOperations[idx];
                    this.Logger.WriteLine($">> [{idx}] '{op.Name}'");
                }

                this.Logger.WriteLine($">> Choose actor to schedule [step '{this.ExploredSteps}']");

                var input = Console.ReadLine();
                if (input.Equals("replay"))
                {
                    if (!this.Replay())
                    {
                        continue;
                    }

                    this.Configuration.SchedulingIterations++;
                    this.PrepareForNextIteration();
                    return false;
                }
                else if (input.Equals("jump"))
                {
                    this.Jump();
                    continue;
                }
                else if (input.Equals("reset"))
                {
                    this.Configuration.SchedulingIterations++;
                    this.Reset();
                    return false;
                }
                else if (input.Length > 0)
                {
                    try
                    {
                        var idx = Convert.ToInt32(input);
                        if (idx < 0)
                        {
                            this.Logger.WriteLine(">> Expected positive integer, please retry ...");
                            continue;
                        }

                        next = enabledOperations[idx];
                        if (next is null)
                        {
                            this.Logger.WriteLine(">> Unexpected id, please retry ...");
                            continue;
                        }
                    }
                    catch (FormatException)
                    {
                        this.Logger.WriteLine(">> Wrong format, please retry ...");
                        continue;
                    }
                }
                else
                {
                    next = enabledOperations[0];
                }

                this.InputCache.Add(input);
                parsed = true;
            }

            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            next = false;
            this.ExploredSteps++;

            var parsed = false;
            while (!parsed)
            {
                if (this.InputCache.Count >= this.ExploredSteps)
                {
                    var step = this.InputCache[this.ExploredSteps - 1];
                    if (step.Length > 0)
                    {
                        next = Convert.ToBoolean(this.InputCache[this.ExploredSteps - 1]);
                    }
                    else
                    {
                        this.InputCache[this.ExploredSteps - 1] = "false";
                    }

                    break;
                }

                this.Logger.WriteLine($">> Choose true or false [step '{this.ExploredSteps}']");

                var input = Console.ReadLine();
                if (input.Equals("replay"))
                {
                    if (!this.Replay())
                    {
                        continue;
                    }

                    this.Configuration.SchedulingIterations++;
                    this.PrepareForNextIteration();
                    return false;
                }
                else if (input.Equals("jump"))
                {
                    this.Jump();
                    continue;
                }
                else if (input.Equals("reset"))
                {
                    this.Configuration.SchedulingIterations++;
                    this.Reset();
                    return false;
                }
                else if (input.Length > 0)
                {
                    try
                    {
                        next = Convert.ToBoolean(input);
                    }
                    catch (FormatException)
                    {
                        this.Logger.WriteLine(">> Wrong format, please retry ...");
                        continue;
                    }
                }

                this.InputCache.Add(input);
                parsed = true;
            }

            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            next = 0;
            this.ExploredSteps++;

            var parsed = false;
            while (!parsed)
            {
                if (this.InputCache.Count >= this.ExploredSteps)
                {
                    var step = this.InputCache[this.ExploredSteps - 1];
                    if (step.Length > 0)
                    {
                        next = Convert.ToInt32(this.InputCache[this.ExploredSteps - 1]);
                    }
                    else
                    {
                        this.InputCache[this.ExploredSteps - 1] = "0";
                    }

                    break;
                }

                this.Logger.WriteLine($">> Choose an integer (< {maxValue}) [step '{this.ExploredSteps}']");

                var input = Console.ReadLine();
                if (input.Equals("replay"))
                {
                    if (!this.Replay())
                    {
                        continue;
                    }

                    this.Configuration.SchedulingIterations++;
                    this.PrepareForNextIteration();
                    return false;
                }
                else if (input.Equals("jump"))
                {
                    this.Jump();
                    continue;
                }
                else if (input.Equals("reset"))
                {
                    this.Configuration.SchedulingIterations++;
                    this.Reset();
                    return false;
                }
                else if (input.Length > 0)
                {
                    try
                    {
                        next = Convert.ToInt32(input);
                    }
                    catch (FormatException)
                    {
                        this.Logger.WriteLine(">> Wrong format, please retry ...");
                        continue;
                    }
                }

                if (next >= maxValue)
                {
                    this.Logger.WriteLine($">> {next} is >= {maxValue}, please retry ...");
                }

                this.InputCache.Add(input);
                parsed = true;
            }

            return true;
        }

        /// <summary>
        /// Forces the next asynchronous operation to be scheduled.
        /// </summary>
        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        public bool PrepareForNextIteration()
        {
            this.ExploredSteps = 0;
            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            this.InputCache.Clear();
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        public int GetScheduledSteps()
        {
            return this.ExploredSteps;
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        public bool HasReachedMaxSchedulingSteps()
        {
            var bound = this.IsFair() ? this.Configuration.MaxFairSchedulingSteps :
                this.Configuration.MaxUnfairSchedulingSteps;

            if (bound == 0)
            {
                return false;
            }

            return this.ExploredSteps >= bound;
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        public bool IsFair() => false;

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public string GetDescription() => string.Empty;

        /// <summary>
        /// Replays an earlier point of the execution.
        /// </summary>
        private bool Replay()
        {
            var result = true;

            this.Logger.WriteLine($">> Replay up to first ?? steps [step '{this.ExploredSteps}']");

            try
            {
                var steps = Convert.ToInt32(Console.ReadLine());
                if (steps < 0)
                {
                    this.Logger.WriteLine(">> Expected positive integer, please retry ...");
                    result = false;
                }

                this.RemoveFromInputCache(steps);
            }
            catch (FormatException)
            {
                this.Logger.WriteLine(">> Wrong format, please retry ...");
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Jumps to a later point in the execution.
        /// </summary>
        private bool Jump()
        {
            var result = true;

            this.Logger.WriteLine($">> Jump to ?? step [step '{this.ExploredSteps}']");

            try
            {
                var steps = Convert.ToInt32(Console.ReadLine());
                if (steps < this.ExploredSteps)
                {
                    this.Logger.WriteLine(">> Expected integer greater than " +
                        $"{this.ExploredSteps}, please retry ...");
                    result = false;
                }

                this.AddInInputCache(steps);
            }
            catch (FormatException)
            {
                this.Logger.WriteLine(">> Wrong format, please retry ...");
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Adds in the input cache.
        /// </summary>
        private void AddInInputCache(int steps)
        {
            if (steps > this.InputCache.Count)
            {
                this.InputCache.AddRange(Enumerable.Repeat(string.Empty, steps - this.InputCache.Count));
            }
        }

        /// <summary>
        /// Removes from the input cache.
        /// </summary>
        private void RemoveFromInputCache(int steps)
        {
            if (steps > 0 && steps < this.InputCache.Count)
            {
                this.InputCache.RemoveRange(steps, this.InputCache.Count - steps);
            }
            else if (steps == 0)
            {
                this.InputCache.Clear();
            }
        }
    }
}
