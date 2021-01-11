// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class MockedManualResetEvent
    {
        internal class AuxData : object
        {
            public bool IsSet { get; set; }
            private readonly Resource Res;

            public AuxData()
            {
                this.IsSet = false;
                this.Res = new Resource();
            }

            public AuxData(bool state)
            {
                this.IsSet = state;
                this.Res = new Resource();
            }

            public void SignalResource()
            {
                // For ManualResetEvent, we will signal all the threads waiting on this resource.
                this.Res.SignalAll();
            }

            public void WaitResource()
            {
                this.Res.Wait();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ChangeMREStatus(ManualResetEvent mre, bool v)
        {
            if (CoyoteRuntime.Current.Cwt.TryGetValue(mre, out object data))
            {
                AuxData d = data as AuxData;
                d.IsSet = v;

                CoyoteRuntime.Current.Cwt.Remove(mre);
                CoyoteRuntime.Current.Cwt.Add(mre, d);

                // If you are setting the MRE to true, signal all the waiting operations.
                if (v)
                {
                    d.SignalResource();
                }
            }
            else
            {
                Debug.Assert(false, "This MRE is not pre-registered. This should not happen.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InitResetEvent(ManualResetEvent mre, bool status)
        {
            if (CoyoteRuntime.Current.IsControlled)
            {
                if (CoyoteRuntime.Current.Cwt.TryGetValue(mre, out _))
                {
                    Debug.Assert(false, $"This should never happen. Init reset event on {mre.ToString()}, duplicate found");
                }

                AuxData d = new AuxData(status);
                CoyoteRuntime.Current.Cwt.Add(mre, d);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ManualResetEvent Create(bool initialState)
        {
            if (CoyoteRuntime.Current.IsControlled)
            {
                var tbr = new ManualResetEvent(initialState);

                // Register this MRE and its initial state
                InitResetEvent(tbr, initialState);
                return tbr;
            }
            else
            {
                return new ManualResetEvent(initialState);
            }
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class MockedEventWaitHandle
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Set(EventWaitHandle o)
        {
            if (o.GetType().FullName == "System.Threading.ManualResetEvent")
            {
                ManualResetEvent mre = o as ManualResetEvent;

                if (CoyoteRuntime.Current.IsControlled)
                {
                    CoyoteRuntime.Current.ScheduleNextOperation();
                    MockedManualResetEvent.ChangeMREStatus(mre, true);
                }

                return mre.Set();
            }
            else
            {
                throw new System.Exception($"Controlled EventWaitHandle.Set() doesn't take {o.GetType().FullName} as input.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Reset(EventWaitHandle o)
        {
            if (o.GetType().FullName == "System.Threading.ManualResetEvent")
            {
                ManualResetEvent mre = o as ManualResetEvent;

                if (CoyoteRuntime.Current.IsControlled)
                {
                    CoyoteRuntime.Current.ScheduleNextOperation();
                    MockedManualResetEvent.ChangeMREStatus(mre, false);
                }

                return mre.Reset();
            }
            else
            {
                throw new System.Exception($"Controlled EventWaitHandle.Reset() doesn't take {o.GetType().FullName} as input.");
            }
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class MockedWaitHandle
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WaitOne(WaitHandle o)
        {
            if (o.GetType().FullName == "System.Threading.ManualResetEvent")
            {
                ManualResetEvent mre = o as ManualResetEvent;

                if (CoyoteRuntime.Current.IsControlled)
                {
                    CoyoteRuntime.Current.ScheduleNextOperation();

                    if (CoyoteRuntime.Current.Cwt.TryGetValue(mre, out object data))
                    {
                        MockedManualResetEvent.AuxData d = data as MockedManualResetEvent.AuxData;
                        Debug.Assert(d != null, "Object returned from Cwt is not AuxData");

                        if (d.IsSet)
                        {
                            return true;
                        }
                        else
                        {
                            d.WaitResource();
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "this MRE event wasn't pre-registered. Can this happen during partial rewriting?");
                    }

                    return true;
                }
                else
                {
                    return mre.WaitOne();
                }
            }
            else
            {
                throw new System.Exception($"Controlled WaitHandle.WaitOne() doesn't take {o.GetType().FullName} as input.");
            }
        }
    }
}
