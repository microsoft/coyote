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
        <p>With Coyote, we envision a future where developing software with any form of concurrency is as natural as developing sequential code. We want to demystify concurrency issues, be it with design or testing or debugging, be it single-box systems or large distributed systems. No more <a href="https://en.wikipedia.org/wiki/Heisenbug">Heisenbugs</a>!</p>
        
        <p>Through rapid design-implement-test cycles, Coyote empowers developers with a quantum leap in productivity. The technology provides programming frameworks that reduce the gap between design and implementation and offers unmatched testing capabilities for weeding out bugs early.</p>

        <p>Coyote has been inspired by years of research at MSR, studying concurrency issues in software. MSR has been a leader in this space for more than a decade with several examples of pioneering research. We've learned from that experience, we build on it and put it forth in a form that can be readily consumed by developers. </p>

        <p>Our goal is to help developers reimagine the way we design and implement distributed services.</p>

---
