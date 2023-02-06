
## State machine demo

<div class="animated_svg" trace="../../../assets/data/Raft.xml" svg="../../../assets/images/Raft.svg">
</div>

{% include 'player-controls.html' %}

This graph was generated from a `coyote test` with the `--xml-trace` and `--actor-graph` options on
a `StateMachine` based coyote application that implements the `Raft` protocol. The fact that coyote
`StateMachines` expose explicit state information makes it possible for `coyote` to visualize what
is going on in a level of detail that is hard to extract from other kinds of C# code. This
illustrates the benefit of this programming model both for software design, as well as
implementation enforcing this design, and testing that can find very hard to find bugs in these
kinds of complex distributed systems.

The trace shows a global order of messages being transferred between the various state machines. The
message exchange is shown to happen one after the other (when in production they may be happening in
parallel). This makes it easy to understand the trace. This trace, in fact, demonstrates a bug where
two Raft `Server` state-machines both end up claiming to be leaders at the same time which is a
violation of Raft's consensus requirements that there be at most one leader at a time.

This animation is slowed down from actual testing speed so you can see what is happening. This
entire test normally takes milliseconds so that Coyote can explore a huge number of possible
tests in a short amount of time.