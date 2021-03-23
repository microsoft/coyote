// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// An interactive scheduling strategy.
    /// </summary>
    internal sealed class InteractiveStrategy : SchedulingStrategy
    {
        /// <summary>
        /// The configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The installed logger.
        /// </summary>
        private readonly ILogger Logger;

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
        internal InteractiveStrategy(Configuration configuration, ILogger logger)
        {
            this.Logger = logger ?? new ConsoleLogger();
            this.Configuration = configuration;
            this.InputCache = new List<string>();
            this.ExploredSteps = 0;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.ExploredSteps = 0;
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            bool isYielding, out AsyncOperation next)
        {
            next = null;

            var enabledOps = new SortedDictionary<ulong, AsyncOperation>();
            foreach (var op in ops.Where(op => op.Status is AsyncOperationStatus.Enabled))
            {
                enabledOps.Add(op.Id, op);
            }

            if (enabledOps.Count is 0)
            {
                this.Logger.WriteLine(">> No available operations to schedule ...");
                return false;
            }

            this.ExploredSteps++;

            var parsed = false;
            while (!parsed)
            {
                if (this.InputCache.Count >= this.ExploredSteps)
                {
                    var step = this.InputCache[this.ExploredSteps - 1];
                    ulong operationId = 0;
                    if (step.Length > 0)
                    {
                        operationId = Convert.ToUInt64(step);
                    }
                    else
                    {
                        this.InputCache[this.ExploredSteps - 1] = $"{operationId}";
                    }

                    next = enabledOps[operationId];
                    parsed = true;
                    break;
                }

                this.Logger.WriteLine(">> Available operations to schedule ...");
                foreach (var op in enabledOps)
                {
                    this.Logger.WriteLine($">> [{op.Key}] {op.Value.Name}");
                }

                this.Logger.WriteLine($">> Choose operation to schedule [step '{this.ExploredSteps}']");

                var input = Console.ReadLine();
                if (input.Equals("replay"))
                {
                    if (!this.Replay())
                    {
                        continue;
                    }

                    this.Configuration.TestingIterations++;
                    this.InitializeNextIteration(this.Configuration.TestingIterations);
                    return false;
                }
                else if (input.Equals("jump"))
                {
                    this.Jump();
                    continue;
                }
                else if (input.Equals("reset"))
                {
                    this.Configuration.TestingIterations++;
                    this.Reset();
                    return false;
                }
                else if (input.Length > 0)
                {
                    try
                    {
                        var operationId = Convert.ToUInt64(input);
                        if (!enabledOps.TryGetValue(operationId, out next))
                        {
                            this.Logger.WriteLine(LogSeverity.Warning, ">> Unexpected operation id, please retry ...");
                            continue;
                        }
                    }
                    catch (FormatException)
                    {
                        this.Logger.WriteLine(LogSeverity.Warning, ">> Expected positive integer, please retry ...");
                        continue;
                    }
                }
                else
                {
                    // If the current operation is enabled, then set it as next, else set the first enabled.
                    next = enabledOps.TryGetValue(current.Id, out AsyncOperation op) ? op : enabledOps.First().Value;
                }

                this.InputCache.Add(input);
                parsed = true;
            }

            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
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

                    this.Configuration.TestingIterations++;
                    this.InitializeNextIteration(this.Configuration.TestingIterations);
                    return false;
                }
                else if (input.Equals("jump"))
                {
                    this.Jump();
                    continue;
                }
                else if (input.Equals("reset"))
                {
                    this.Configuration.TestingIterations++;
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
                        this.Logger.WriteLine(LogSeverity.Warning, ">> Expected boolean value, please retry ...");
                        continue;
                    }
                }

                this.InputCache.Add(input);
                parsed = true;
            }

            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
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

                    this.Configuration.TestingIterations++;
                    this.InitializeNextIteration(this.Configuration.TestingIterations);
                    return false;
                }
                else if (input.Equals("jump"))
                {
                    this.Jump();
                    continue;
                }
                else if (input.Equals("reset"))
                {
                    this.Configuration.TestingIterations++;
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
                        this.Logger.WriteLine(LogSeverity.Warning, ">> Expected integer, please retry ...");
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

        /// <inheritdoc/>
        internal override int GetScheduledSteps()
        {
            return this.ExploredSteps;
        }

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached()
        {
            var bound = this.IsFair() ? this.Configuration.MaxFairSchedulingSteps :
                this.Configuration.MaxUnfairSchedulingSteps;

            if (bound is 0)
            {
                return false;
            }

            return this.ExploredSteps >= bound;
        }

        /// <inheritdoc/>
        internal override bool IsFair() => false;

        /// <inheritdoc/>
        internal override string GetDescription() => string.Empty;

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
                    this.Logger.WriteLine(LogSeverity.Warning, ">> Expected positive integer, please retry ...");
                    result = false;
                }

                this.RemoveFromInputCache(steps);
            }
            catch (FormatException)
            {
                this.Logger.WriteLine(LogSeverity.Warning, ">> Wrong format, please retry ...");
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
                    this.Logger.WriteLine(LogSeverity.Warning, ">> Expected integer greater than " +
                        $"{this.ExploredSteps}, please retry ...");
                    result = false;
                }

                this.AddInInputCache(steps);
            }
            catch (FormatException)
            {
                this.Logger.WriteLine(LogSeverity.Warning, " >> Wrong format, please retry ...");
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
            else if (steps is 0)
            {
                this.InputCache.Clear();
            }
        }

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.InputCache.Clear();
            this.ExploredSteps = 0;
        }
    }
}
