---
layout: reference
section: learn
title: Using async/await in a machine
permalink: /learn/core-concepts/using-async
---

Using async/await in a machine
==============================
Coyote supports the use of `async`/`await` style programming that is now mainstream in C# programs. This document outlines Coyote's support as well as coding guidelines while using `async`/`await` code.

## Async APIs
Some of the Coyote runtime APIs are awaitable to make them non-blocking operations. Most notable of these APIs is `Receive`:
```c#
async Task<Event> Receive(params Type[] eventTypes) {}
```
Internally, this is implemented by invoking `await` on a per-machine `TaskCompletionSource<Event>`. This completion source is set to complete whenever the machine receives an event of the waiting-to-receive type (or is synchronously set to completion if it already has such an event in the queue when `Receive` is invoked).

## Async action handlers in machines
Action handlers in a Coyote machine can either be `void`:
```c#
void Foo() { ... }
```
Or `async`:
```c#
async Task Foo() { ... }
```
Coyote async action handlers can only return a `Task` (neither `void` nor `Task<T>` is allowed). Return type `void` is not allowed because exceptions are not propagated to the caller (and its necessary for Coyote to be able to catch exceptions in user code!), whereas `Task<T>` is not allowed for the same reason that original action handlers can only be `void`.

The main purpose of the new async action handlers is to allow the user to invoke `async` methods and `await` on them to enable non-blocking asynchronous execution of a machine:
```c#
async Task Foo() {
  await this.Receive(typeof(SomeEvent));
}
```

## Coyote Syntax
Anonymous action handlers can _optionally_ be declared as `async`:
```c#
on e do async { ... await ... }
```
Annotating an anonymous action handler with the `async` keyword allows us to generate an action (during rewriting) that is declared as `async Task`.

## Usage rules
- Any `async` method, when called, must be `awaited`. Otherwise, it can lead to the creation of a `Task` that is not under the control of `CoyoteTester`.
- Never use `await(...).ConfigureAwait(false)` in user code, because this can cause the continuation from `await` to execute in a different synchronization context, outside the control of `CoyoteTester`.
