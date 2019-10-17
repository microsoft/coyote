Coyote (previously known as P#) is a framework for rapid development of reliable asynchronous software. Coyote is used by several teams in [Azure](https://azure.microsoft.com/) to design, implement and automatically test production distributed systems and services.

See our [lovely website](https://microsoft.github.io/microsoft/coyote) for more
information about the project, case studies, and reference documentation.

# Why should I use Coyote?

The key value of Coyote is that it allows you to easily test your code against concurrency and nondeterminism, as well as write and check safety and liveness specifications. During testing, Coyote serializes your program, captures and controls all (implicit as well as specified) nondeterminism, and thoroughly explores the executable code (in your local dev machine) to automatically discover deep concurrency bugs. If a bug is found, Coyote reports a reproducible bug trace that provides a global order of all asynchrony and events in the system, and thus is significantly easier to debug that regular unit-/integration-tests and logs from production or stress tests, which are typically nondeterministic.

Besides testing, Coyote can be directly used in production as it offers fast, efficient and scalable execution. As a testament of this, Coyote is being used by several teams in Azure to build mission-critical services.

# Supported programming models in Coyote
For designing and implementing reliable asynchronous software, Coyote provides the following two programming models:
- **Asynchronous tasks**, which follows the [task-based asynchronous pattern](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap). This programming model is based on the `ControlledTask` type, a drop-in replacement type for `System.Threading.Tasks.Task` that can be controlled by Coyote during testing.
- **Asynchronous communicating state-machines**, an [actor-based programming model](https://en.wikipedia.org/wiki/Actor_model) that allows you to express your design and concurrency at a higher-level. This programming model is based on the `Machine` type, which represents an asynchronous entity that can create new machines, send events to other machines, and handle received events with user-specified logic.

# Getting started
TODO.

# Publications
List of publications on Coyote:
- **[Asynchronous Programming, Analysis and Testing with State Machines](https://dl.acm.org/citation.cfm?id=2737996)**. Pantazis Deligiannis, Alastair F. Donaldson, Jeroen Ketema, Akash Lal and Paul Thomson. In the *36th Annual ACM SIGPLAN Conference on Programming Language Design and Implementation* (PLDI), 2015.
- **[Uncovering Bugs in Distributed Storage Systems During Testing (not in Production!)](https://www.usenix.org/node/194442)**. Pantazis Deligiannis, Matt McCutchen, Paul Thomson, Shuo Chen, Alastair F. Donaldson, John Erickson, Cheng Huang, Akash Lal, Rashmi Mudduluru, Shaz Qadeer and Wolfram Schulte. In the *14th USENIX Conference on File and Storage Technologies* (FAST), 2016.
- **[Lasso Detection using Partial-State Caching](https://www.microsoft.com/en-us/research/publication/lasso-detection-using-partial-state-caching-2/)**. Rashmi Mudduluru, Pantazis Deligiannis, Ankush Desai, Akash Lal and Shaz Qadeer. In the *17th International Conference on Formal Methods in Computer-Aided Design* (FMCAD), 2017.
- **Reliable State Machines: A Framework for Programming Reliable Cloud Services**. Suvam Mukherjee, Nitin John Raj, Krishnan Govindraj, Pantazis Deligiannis, Chandramouleswaran Ravichandran, Akash Lal, Aseem Rastogi and Raja Krishnaswamy. In the *33rd European Conference on Object-Oriented Programming* (ECOOP), 2019.

# Contributing
This project welcomes contributions and suggestions. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repositories using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
