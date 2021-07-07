// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Tests.Common.Actors
{
    public class EventGroupList : AwaitableEventGroup<List<string>>
    {
        public List<string> Items = new List<string>();

        public void AddItem(string msg)
        {
            lock (this.Items)
            {
                this.Items.Add(msg);
            }
        }

        public void Complete()
        {
            this.SetResult(this.Items);
        }

        public override string ToString()
        {
            string result = null;
            lock (this.Items)
            {
                result = string.Join(", ", this.Items);
            }

            return result;
        }
    }

    public class TraceableStateMachine : StateMachine
    {
        protected EventGroupList TraceOp;

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            this.TraceOp = this.CurrentEventGroup as EventGroupList;
            this.Assert(this.TraceOp != null, "Did you forget to provide OperationTrace?");
            return base.OnInitializeAsync(initialEvent);
        }

        protected void Trace(string format, params object[] args)
        {
            var msg = string.Format(format, args);
            this.TraceOp.AddItem(msg);
        }

        private protected override void OnStateChanged()
        {
            if (this.TraceOp != null && !string.IsNullOrEmpty(this.CurrentStateName))
            {
                this.Trace("CurrentState={0}", this.CurrentStateName);
            }

            base.OnStateChanged();
        }

        protected void OnFinalEvent()
        {
            this.TraceOp.SetResult(this.TraceOp.Items);
        }
    }
}
