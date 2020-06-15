// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Actors
{
    public class QuiescentOperationTests : BaseProductionTest
    {
        public QuiescentOperationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SendCountEvent : Event
        {
            public int Count;
        }

        private class E : Event
        {
            public ActorId Id;

            public E()
            {
            }

            public E(ActorId id)
            {
                this.Id = id;
            }
        }

        private class QMachine : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(Busy))]
            public class Init : State
            {
            }

            public class Busy : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSimpleQuiescentOperation()
        {
            this.Test(async r =>
            {
                var q = new QuiescentOperation();
                var a = r.CreateActor(typeof(QMachine), null, q);
                r.SendEvent(a, new E());
                var result = await this.GetResultAsync(q.Task);
                Assert.True(result);
            });
        }

        //----------------------------------------------------------------------------------------------------
        [OnEventDoAction(typeof(E), nameof(HandleE))]
        internal class QReceiver : Actor
        {
            private SendCountEvent Counter;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                this.Counter = (SendCountEvent)e;
                return base.OnInitializeAsync(e);
            }

            private void HandleE()
            {
                this.Counter.Count--;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestQuiescentSends()
        {
            this.Test(async r =>
            {
                var c = new SendCountEvent() { Count = 100 };
                var q = new QuiescentOperation();
                var a = r.CreateActor(typeof(QReceiver), c, q);
                for (int i = c.Count; i > 0; i--)
                {
                    r.SendEvent(a, new E());
                }

                // note the QuiescentOperation allows us to easily discover when
                // the actor has handled all the events we just sent without having
                // to modify the actor class to make it know about this Operation.
                var result = await this.GetResultAsync(q.Task);
                Assert.True(result);
                Assert.Equal(0, c.Count);
            });
        }

        internal class TestException : Exception
        {
            public TestException(string msg)
                : base(msg)
            {
            }
        }

        //----------------------------------------------------------------------------------------------------
        [OnEventDoAction(typeof(E), nameof(HandleE))]
        internal class QError : Actor
        {
            private void HandleE()
            {
                throw new TestException("this is a bug");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestQuiescentOnError()
        {
            this.TestWithException<TestException>(async r =>
            {
                var q = new QuiescentOperation();
                var a = r.CreateActor(typeof(QError));
                for (int i = 0; i < 100; i++)
                {
                    r.SendEvent(a, new E(), q);
                }

                // note the QuiescentOperation also terminates on unhandled exceptions inside the actor.
                await this.GetResultAsync(q.Task);
            });
        }
    }
}
