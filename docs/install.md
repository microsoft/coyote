---
title: Get started with Coyote
permalink: /install
layout: tabs
body-class: gray
tabs:
  - title: Install package
    content: |
        <a href="https://www.nuget.org/packages/Microsoft.PSharp/" class="btn btn-primary mt-50 mb-20">Install package </a>
        <h2>Different ways to use Coyote</h2>
        <p>Coyote is built on top of the .NET framework and the Roslyn compiler.
        </p>
        <p>Coyote is provided as both a language extension of C#, as well as a set of library and runtime APIs that can be directly used from inside a C# program. This means that there are two main ways someone can use Psharp to build highly reliable systems:</p>
        <ol>
            <li>The surface syntax of Coyote (i.e., C# language extension) can be used to build an entire system from scratch (see an example here). The surface Psharp syntax directly extends C# with new language constructs, which allows for rapid prototyping. However, to use the surface syntax, a developer has to use the Coyote compiler, which is built on top of Roslyn. The main disadvantage of this approach is that Coyote does not yet fully integrate with the Visual Studio integrated development environment (IDE), although we are actively working on this (see here), and thus does not support high-productivity features such as IntelliSense (e.g., for auto-completition and automated refactoring)
            </li>
            <li>The Coyote library and runtime APIs (available for C#) can be used to build an entire system from scratch (see an example here). This approach is slightly more verbose than the above, but allows full integration with Visual Studio.</li>
        </ol>
        <p>Coyote can be also used for thoroughly testing an existing message-passing system, by modeling its environment (e.g. a client) and/or components of the system. However, this approach has the disadvantage that if nondeterminism in the system is not captured by (or expressed in) Coyote, then the Coyote testing engine might be unable to discover and reproduce bugs.
        </p>
        <p>Note that many examples in our documentation will use the Coyote surface syntax, since it is less verbose.</p>
  - title: Build from source
    content: |
        <h2>P Sharp prerequisites</h2>
        <p> Install <a href="https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio" target="_blank">Visual Studio 2017</a> and .<a href="">NET Core</a> (available as an optional component during the VS 2017 installation, or can independently install the SDK from <a href="">here</a>). Also install all the SDK versions of the .NET Framework that P# currently supports (4.5 and 4.6) from <a href="">here</a>.</p>
        <p>Optional: Get the <a href="">Visual Studio 2017 SDK</a> to be able to compile the P# visual studio extension (syntax highlighting). Only for the high-level P Sharp language.</p>
        <h2>Building the P Sharp project</h2>
        <p>To build P Sharp, either open PSharp.sln and build from inside Visual Studio 2017 (you may need to run dotnet restore from the command line prior to opening the solution in order to successfully compile), or run the following powershell script (available in the root directory) from the Visual Studio 2017 developer command prompt:</p>
        <code id="build" data-clipboard-target="#build"  data-toggle="tooltip" data-placement="top" title="copied!">powershell -c .\Scripts\build.ps1</code>
        <h2>Building the samples</h2>
        <p>To build the samples, run the above script with the samples option:</p>
        <code id="build-samples" data-clipboard-target="#build-samples"  data-toggle="tooltip" data-placement="top" title="copied!">>powershell -c .\build.ps1 -samples</code>
        <h2>Running the tests</h2>
        <p>To run all available tests, execute the following powershell script (available in the Scripts directory):</p>
        <code id="run-all" data-clipboard-target="#run-all"  data-toggle="tooltip" data-placement="top" title="copied!">.\Scripts\run-tests.ps1</code>
        <p>To run only a specific category of tests, use the -test option to specify the category name, for example:</p>
        <code id="run-category" data-clipboard-target="#run-category"  data-toggle="tooltip" data-placement="top" title="copied!">.\Scripts\run-tests.ps1 -test core </code>
        
---
