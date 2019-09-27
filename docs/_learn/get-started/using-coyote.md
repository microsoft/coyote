---
layout: reference
section: learn
title: Using Coyote
permalink: /learn/get-started/using-coyote
---

# Using Coyote—the different ways
Coyote is built on top of the .NET framework and the Roslyn compiler.

Coyote is provided as both a language extension of C#, as well as a set of library and runtime APIs that can be directly used from inside a C# program. This means that there are two main ways someone can use Coyote to build highly reliable systems:
- The surface syntax of Coyote (i.e., C# language extension) can be used to build an entire system from scratch (see an example here). The surface Coyote syntax directly extends C# with new language constructs, which allows for rapid prototyping. However, to use the surface syntax, a developer has to use the Coyote compiler, which is built on top of Roslyn. The main disadvantage of this approach is that Coyote does not yet fully integrate with the Visual Studio integrated development environment (IDE), although we are actively working on this (see here), and thus does not support high-productivity features such as IntelliSense (e.g., for auto-completion and automated refactoring) 
- The Coyote library and runtime APIs (available for C#) can be used to build an entire system from scratch (see an example here). This approach is slightly more verbose than the above, but allows full integration with Visual Studio.

Coyote can be also used for thoroughly testing an existing message-passing system, by modeling its environment (e.g. a client) and/or components of the system. However, this approach has the disadvantage that if nondeterminism in the system is not captured by (or expressed in) Coyote, then the Coyote testing engine might be unable to discover and reproduce bugs. 

Note that many examples in our documentation will use the Coyote surface syntax, since it is less verbose.
