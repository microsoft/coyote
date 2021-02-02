# Welcome to Coyote

Coyote is a set of .NET libraries and tools designed to help ensure that your code is free of bugs.
Too often developers are drowning in the complexity of their own code and many hours are wasted
trying to track down impossible-to-find bugs, especially when dealing with _concurrent_ code or
various other sources of _non-determinism_ (like message ordering, failures, timeouts and so on).

Coyote provides programming models to express concurrent systems. These programming models offer
convenient ways to program at a high-level of abstraction. As mentioned below, Coyote currently
supports two programming models: a familiar tasks-based programming model (currently in-preview) as
well as a more advanced actor-based programming model. These programming models are built using
asynchronous APIs, supported by a lightweight runtime, making it easy to program efficient
non-blocking code.

<div class="embed-responsive embed-responsive-16by9">
    <video id="shortintro" class="embed-responsive-item" controls poster="assets/images/ShortIntro.png">
        <source  class="embed-responsive-item" src="https://github.com/microsoft/coyote-content/raw/master/assets/video/ShortIntro.mp4" type="video/mp4">
        <!-- <source src="/assets/ShortIntro.webm" type="video/webm"> -->
    </video>
</div>

<div id="caption" style="background:#151520; color:white; font-size: 18px; padding:5px;"></div>
<br/>

<script type="text/javascript">

  var captions = [[0, "This animation shows messages passing through a highly parallel distributed system."],
              [5, "Each node represents a microservice or a piece of code running on some machine."],
              [10, "Messages are flying through this system in a way that makes it hard to debug when something goes wrong."],
              [16, "Coyote tests one async path at a time exploring all possible paths through the system and it does this very quickly"],
              [23, "It also records this path so that when it finds a bug that bug is 100% reproducible."]
          ];

  function show_captions(video, caption){
    var time = video.currentTime;
    var line = null;
    for (var i = 0; i < captions.length; i++) {
      var nextline = captions[i];
      if (nextline[0] > time) break;
      line = nextline;
    }
    if (line != null) {
      caption.style.display="block";
      caption.innerHTML = line[1]
    } else {
      caption.style.display="none";
    }
  }

  $(document).ready(function () {
      video  = $("#shortintro")[0];
      caption = $("#caption")[0];
      caption.style.display="none";
      video.ontimeupdate = function() { show_captions(video, caption); };
  });

</script>

Coyote helps write powerful, expressive tests for your code. You can declare sources of
non-determinism (such as timers, failures, etc.) as part of your tests. The Coyote testing tool can
_systematically_ explore a large number of interleavings of concurrent operations as well as
non-deterministic choices so that it covers a large set of behaviors in a very short time. This is
different from _stress testing_. Coyote takes control of the concurrency so that it can manipulate
every possible scheduling. With appropriate _mocking_, Coyote can also do this in "developer" mode
on a single laptop with little or no dependence on the bigger production environment.

Coyote is not a verification system. It does not use theorem proving to make correctness guarantees,
instead it uses intelligent search strategies to drive systematic testing, based on deep
understanding of concurrency primitives that you have used in your code. This approach has proven to
work well for large production teams, including many teams in Microsoft Azure because it has a small
barrier to entry with almost immediate benefits for those who adopt it.

Coyote does not require that a team starts from scratch and rebuild their system. Often it is too
expensive to start over. Instead Coyote can be adopted gradually, adding more and more structure
around your _Coyote-aware_ code. The more of this structure you add the more benefit you get from
Coyote, but it is certainly not an all or nothing proposition.

So Coyote brings together elements of design, development and testing into an integrated package
that works really well in the real world. See our [case
studies](case-studies/azure-batch-service.md) for some great customer testimonials.

## Supported programming models

Coyote supports two main programming models:

- [Asynchronous tasks](programming-models/async/overview.md), which follows the popular [task-based
  asynchronous
  pattern](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap).
  This programming model offers a `Task` type  that serves as a drop-in-replacement type for the
  native .NET `System.Threading.Tasks.Task`. As with the native `Task`, a Coyote `Task` represents
  an asynchronous operation that the programmer can coordinate using the `async` and `await`
  keywords of [C#](https://docs.microsoft.com/en-gb/dotnet/csharp/). In production, a Coyote `Task`
  executes with the same semantics of a native `Task`. In fact, it is simply a thin wrapper around a
  native `Task` object. During testing, however, is where the magic happens. Coyote controls the
  execution of each Coyote `Task` so that it can explore various different interleavings to find
  bugs. Alternatively, you can use the [binary rewriting](programming-models/async/rewriting.md)
  feature of Coyote to automatically instrument your application, taking control of
  `System.Threading.Tasks.Task` objects and related concurrency types from the Task Parallel
  Library, without having to use Coyote's drop-in-replacement library.

- [Asynchronous actors](programming-models/actors/overview.md) is an [actor-based programming
  model](https://en.wikipedia.org/wiki/Actor_model) that allows you to express your design and
  concurrency at a higher-level of abstraction. This programming model starts with the `Actor` type
  that represents a long-lived, interactive asynchronous object. An actor can create new actors,
  send events to other actors, and handle received events. This more advanced programming model is
  ideal for cases when asynchronous tasks get too unwieldy. This programming model also provides a
  `StateMachine` type for easy development of event-driven state-machines. A `StateMachine` is
  simply an `Actor` with explicit `States` and event-driven state transitions.

Note that you can only systematically test the above two programming models together using Coyote's
[binary rewriting](programming-models/async/rewriting.md) to take control of
`System.Threading.Tasks.Task` objects. Mixing the Coyote `Actor` type with the custom Coyote `Task`
type is not supported.




## Fearless coding for concurrent software

As a result Coyote gives your team much more confidence in building mission-critical services that
also push the limits on high concurrency, maximizing throughput and minimizing operational costs.

With Coyote you can create highly reliable software in a way that is also highly productive.

These are some direct quotes from Azure Engineers that uses Coyote:

  * _We often found bugs with Coyote in a matter of minutes that would have taken days with stress testing._

  * _Coyote added agility and allowed progress at a much faster pace._

  * _Features were developed in a test environment to first pass the Coyote tester. When dropped in
  production, they simply worked from the start._

  * _Coyote gave developers a significant confidence boost by providing full failover and
  concurrency testing at each check-in, right on their desktops as the code was written._

[Learn more about Coyote](overview/about.md)

[Get started now, it is super easy](get-started/install.md)

[Read more about how Azure is using Coyote](case-studies/azure-batch-service.md)

[State machine demo](programming-models/actors/state-machine-demo/)

[Code on Github](https://github.com/microsoft/coyote/)

