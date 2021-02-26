# Concurrency Unit Testing with Coyote

Coyote is .NET library and tool designed to help ensure that your code is free of concurrency bugs.

Too often developers are drowning in the complexity of their own code and many hours are wasted
trying to track down impossible-to-find bugs, especially when dealing with _concurrent_ code or
various other sources of _non-determinism_ (like message ordering, failures, timeouts and so on).

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

Coyote helps write powerful, expressive tests for your code. We call these _concurrency unit tests_.
You can declare sources of non-determinism (such as timeouts and failures) as part of your Coyote
tests. The Coyote testing tool can _systematically_ explore a large number of interleavings of
concurrent operations as well as non-deterministic choices so that it covers a large set of
behaviors in a very short time. This is different from _stress testing_. Coyote takes control of the
concurrency so that it can manipulate every possible scheduling. With appropriate _mocking_, Coyote
can also do this in "developer" mode on a single laptop with little or no dependence on the bigger
production environment.

Coyote is not a verification system. It does not use theorem proving to make correctness guarantees,
instead it uses intelligent search strategies to drive systematic testing, based on deep
understanding of concurrency primitives that you have used in your code. This approach has proven to
work well for large production teams, including many teams in Microsoft Azure because it has a small
barrier to entry with almost immediate benefits for those who adopt it.

Coyote does not require that a team starts from scratch and rebuilds their system. Coyote uses
binary rewriting during test time to take control of the concurrency in your _unmodified_ code. For
advanced users, Coyote also provides a powerful in-memory actor and state machine programming model
that allows you to build reliable concurrent systems from the ground up. This programming model
allows you to program at a high-level of abstraction. Coyote actors are built using
asynchronous C# APIs, supported by a lightweight runtime, making it easy to program efficient
non-blocking code.

So Coyote brings together elements of design, development and testing into an integrated package
that works really well in the real world. See our [case
studies](case-studies/azure-batch-service.md) for some great customer testimonials.

## Fearless coding for concurrent software

Using Coyote gives your team much more confidence in building mission-critical services that
also push the limits on high concurrency, maximizing throughput and minimizing operational costs.

With Coyote you can create highly reliable software in a way that is also highly productive.

These are some direct quotes from Azure Engineers that use Coyote:

  * _We often found bugs with Coyote in a matter of minutes that would have taken days with stress testing._

  * _Coyote added agility and allowed progress at a much faster pace._

  * _Features were developed in a test environment to first pass the Coyote tester. When dropped in
  production, they simply worked from the start._

  * _Coyote gave developers a significant confidence boost by providing full failover and
  concurrency testing at each check-in, right on their desktops as the code was written._

## Explore Coyote

Get started with the following links:

[Learn about the key benefits of using Coyote](overview/benefits.md)

[Install the NuGet package and CLI tool, it is super easy](get-started/install.md)

[Read how various Azure teams are using Coyote](case-studies/azure-batch-service.md)

[Write your first concurrency unit test with Coyote](tutorials/first-concurrency-unit-test.md)

[Check out this cool demo showing Coyote in practice](advanced-topics/actors/state-machine-demo/)

[Learn the core concepts behind Coyote](concepts/non-determinism.md)

[Say hello on Gitter](https://gitter.im/Microsoft/coyote)

[Contribute on Github](https://github.com/microsoft/coyote/)
