---
title: About Us
layout: page
template: grid
layout_gray: true
permalink: /about
section: about
article:
  - title: Origin story
    feature-img: /assets/images/about.jpg
    content: |
        <p>Coyote’s inspiration springs from recognizing the ironic absence of tooling for developing concurrent systems.</p>

        <p>Despite concurrency being a fundamental paradigm of computer science—all layers of a computer system are concurrent, starting from the hardware to the operating system, to applications, distributed systems, and cloud—not much attention has previously been given to the problem of testing these systems for correctness. Current practices dictate that unit tests be deterministic, often leading to the oblique art of stress testing to weed out bugs.</p>

        <p>Our initial thinking was to simply build tools focused on testing, but we quickly realized any technique focused only on testing would have severely limited value: Developers want to write elegant, correct, and performant code on the first go—they don’t want to be tracing buggy interleavings late into the wee hours.</p>

        <p>With this broader vision, Coyote’s programming models marry design, implementation, and testing for remarkable productivity.</p>

        <p>From the moment various Azure teams began using our framework, we’ve been receiving <a href="/coyote/case-studies" target="_blank">enthusiastic feedback</a>. What started as one service using Coyote has now expanded to more than ten and growing.</p>

        <p>We would love to make you a part of our story—<a href="https://github.com/microsoft/coyote" target="_blank">join our developer community</a> today.</p>
  - title: Vision
    feature-img: /assets/images/vision.jpg
    content: |
        <p>With Coyote, we envision a future where developers can focus on coding only. Through rapid design-implement-test cycles, Coyote empowers developers with a quantum leap in productivity. The technology provides programming frameworks that reduce the gap between design and implementation and offers unmatched testing capabilities for weeding out bugs early.</p>

        <p>Our goal is to help developers reimagine the way we design and implement asynchronous or distributed systems.</p>

        <p>[TODO -- talk about MSR and how we build on years of experience in testing and formal methods and software engineering]</p>
---
