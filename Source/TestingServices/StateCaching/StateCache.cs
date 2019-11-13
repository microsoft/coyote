// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

using Microsoft.Coyote.IO;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.TestingServices.Tracing.Schedule;

using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.TestingServices.StateCaching
{
    /// <summary>
    /// Class implementing a state cache.
    /// </summary>
    internal sealed class StateCache
    {
        /// <summary>
        /// The testing runtime.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// Set of fingerprints.
        /// </summary>
        private readonly HashSet<Fingerprint> Fingerprints;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateCache"/> class.
        /// </summary>
        internal StateCache(SystematicTestingRuntime runtime)
        {
            this.Runtime = runtime;
            this.Fingerprints = new HashSet<Fingerprint>();
        }

        /// <summary>
        /// Captures a snapshot of the program state.
        /// </summary>
        internal bool CaptureState(out State state, out Fingerprint fingerprint, Dictionary<Fingerprint, List<int>> fingerprintIndexMap,
            ScheduleStep scheduleStep, List<Monitor> monitors)
        {
            fingerprint = this.Runtime.GetProgramState();
            var enabledActorIds = this.Runtime.Scheduler.GetEnabledOperationIds();
            state = new State(fingerprint, enabledActorIds, GetMonitorStatus(monitors));

            if (Debug.IsEnabled)
            {
                if (scheduleStep.Type == ScheduleStepType.SchedulingChoice)
                {
                    Debug.WriteLine(
                        "<LivenessDebug> Captured program state '{0}' at scheduling choice.", fingerprint.GetHashCode());
                }
                else if (scheduleStep.Type == ScheduleStepType.NondeterministicChoice &&
                    scheduleStep.BooleanChoice != null)
                {
                    Debug.WriteLine(
                        "<LivenessDebug> Captured program state '{0}' at nondeterministic choice '{1}'.",
                        fingerprint.GetHashCode(), scheduleStep.BooleanChoice.Value);
                }
                else if (scheduleStep.Type == ScheduleStepType.FairNondeterministicChoice &&
                    scheduleStep.BooleanChoice != null)
                {
                    Debug.WriteLine(
                        "<LivenessDebug> Captured program state '{0}' at fair nondeterministic choice '{1}-{2}'.",
                        fingerprint.GetHashCode(), scheduleStep.NondetId, scheduleStep.BooleanChoice.Value);
                }
                else if (scheduleStep.Type == ScheduleStepType.NondeterministicChoice &&
                    scheduleStep.IntegerChoice != null)
                {
                    Debug.WriteLine(
                        "<LivenessDebug> Captured program state '{0}' at nondeterministic choice '{1}'.",
                        fingerprint.GetHashCode(), scheduleStep.IntegerChoice.Value);
                }
            }

            var stateExists = this.Fingerprints.Contains(fingerprint);
            this.Fingerprints.Add(fingerprint);
            scheduleStep.State = state;

            if (!fingerprintIndexMap.ContainsKey(fingerprint))
            {
                var hs = new List<int> { scheduleStep.Index };
                fingerprintIndexMap.Add(fingerprint, hs);
            }
            else
            {
                fingerprintIndexMap[fingerprint].Add(scheduleStep.Index);
            }

            return stateExists;
        }

        /// <summary>
        /// Returns the monitor status.
        /// </summary>
        private static Dictionary<Monitor, MonitorStatus> GetMonitorStatus(List<Monitor> monitors)
        {
            var monitorStatus = new Dictionary<Monitor, MonitorStatus>();
            foreach (var monitor in monitors)
            {
                MonitorStatus status = MonitorStatus.None;
                if (monitor.IsInHotState())
                {
                    status = MonitorStatus.Hot;
                }
                else if (monitor.IsInColdState())
                {
                    status = MonitorStatus.Cold;
                }

                monitorStatus.Add(monitor, status);
            }

            return monitorStatus;
        }
    }
}
