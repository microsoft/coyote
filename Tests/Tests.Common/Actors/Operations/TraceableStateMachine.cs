// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Tests.Common.Actors.Operations
{
    public class OperationTrace : Operation<bool>
    {
        public List<string> Trace = new List<string>();

        public void WriteLine(string msg, params object[] args)
        {
            this.Trace.Add(string.Format(msg, args));
        }

        public override string ToString()
        {
            return string.Join(", ", this.Trace);
        }
    }

    public class TraceableStateMachine : StateMachine
    {
        protected OperationTrace TraceOp;

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            this.TraceOp = this.CurrentOperation as OperationTrace;
            this.Assert(this.TraceOp != null, "Did you forget to provide OperationTrace?");
            return base.OnInitializeAsync(initialEvent);
        }

        protected void Trace(string msg, params object[] args)
        {
            this.TraceOp.WriteLine(msg, args);
        }

        protected override void OnStateChanged()
        {
            if (this.TraceOp != null && !string.IsNullOrEmpty(this.CurrentStateName))
            {
                this.Trace("CurrentState={0}", this.CurrentStateName);
            }

            base.OnStateChanged();
        }

        protected void OnFinalEvent()
        {
            this.TraceOp.SetResult(true);
        }
    }
}
