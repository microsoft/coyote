
## Generate DGML diagrams

Coyote generates Directed Graph Markup Language ([DGML](https://en.wikipedia.org/wiki/DGML))
diagrams showing the states and events discovered during testing of a state machine. DGML diagrams
can be viewed using Visual Studio if you have the `DGML Editor` feature installed.

The `DGML Editor` feature of Visual Studio 2019 can be installed if you click Modify on the Visual
Studio Installer, select "Additional Components" tab and scroll down to "DGML Editor" and make sure
it is checked.

The Enterprise version of Visual Studio can also produce `DGML` diagrams of your code, this is how
the following diagrams were produced for the [Raft Mocking Example](../tutorials/actors/raft-mocking.md):

![RaftMocking](../assets/images/RaftMocking.svg)
