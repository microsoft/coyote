// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tasks;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Tests.Common.Actors.Operations
{
    public class OperationList : Operation<bool>
    {
        public List<string> Items = new List<string>();

        public void AddItem(string msg)
        {
            this.Items.Add(msg);
        }

        public async Task<string> WaitForResult(int millisecondsTimeout = 5000)
        {
            Task timeout = Task.Delay(millisecondsTimeout);
            var t = await Task.WhenAny(this.Completion.Task, timeout);
            if (!this.Completion.Task.IsCompleted)
            {
                throw new TimeoutException("Timeout waiting for OperationList result, results so far: " + this.ToString());
            }

            return this.ToString();
        }

        public override string ToString()
        {
            return string.Join(", ", this.Items);
        }
    }

    public class TraceableStateMachine : StateMachine
    {
        protected OperationList TraceOp;

        protected override SystemTasks.Task OnInitializeAsync(Event initialEvent)
        {
            this.TraceOp = this.CurrentOperation as OperationList;
            this.Assert(this.TraceOp != null, "Did you forget to provide OperationTrace?");
            return base.OnInitializeAsync(initialEvent);
        }

        protected void Trace(string format, params object[] args)
        {
            var msg = string.Format(format, args);
            this.TraceOp.AddItem(msg);
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
