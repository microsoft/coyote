## CloudMessaging

This sample is made up of the following parts:

- [Raft](./CloudMessaging/Raft) - a core C# class library that implements the [Raft Consensus Algorithm](https://raft.github.io/) using the Coyote [Actor Programming Model](https://microsoft.github.io/coyote/concepts/actors/overview).
- [Raft.Azure](./CloudMessaging/Raft.Azure) - a C# executable that shows how to run Coyote messages through an [Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus/).
- [Raft.Mocking](./CloudMessaging/Raft.Mocking): demonstrates how to use mocks to systematically test in-memory the [CloudMessaging](./CloudMessaging) sample application.
- [Raft.Nondeterminism](./CloudMessaging/Raft.Nondeterminism): demonstrates how to introduce controlled nondeterminism in your Coyote tests to systematically exercise corner-cases.

See the following tutorial content that goes with this sample:

- [Raft consensus protocol on Azure](https://microsoft.github.io/coyote/tutorials/actors/raft-azure).
- [Raft consensus protocol with mocks for testing](https://microsoft.github.io/coyote/tutorials/actors/raft-mocking).