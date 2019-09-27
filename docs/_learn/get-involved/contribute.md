---
layout: reference
section: learn
title: Opening a pull request--simple rules
permalink: /learn/get-involved/contribute
---

Opening a pull request--simple rules
==================
You are welcome to contribute to Coyote with bug fixes or new features! If you want to contribute, please open a pull request (PR), so that we can code review your changes, discuss them, and track their progress.

[Coyote pull request](https://github.com/microsoft/coyote/pulls){:.btn.btn-primary}

## Naming branches
Always develop new features in a separate branch (master is protected), and then ask (via a PR) to review and merge (if the PR is accepted) with master. Please name feature branches as `<username>/<feature>`.

## Coding Guidelines
Note that we expect you to carefully follow the following coding guidelines, and of course use common sense.

- Use similar coding conventions as the rest of the project. The basic rule to remember is to write code in the same style as the existing/surrounding code.
- Name variables with sensible names. Use the same style and conventions as the rest of the project (e.g. regarding `PascalCase` and `camelCase`).
- Always use brackets in `if`, `else` and the rest of the control statements, even if you are just writing one single statement. It is easier to maintain and less error-prone.
- Document your code as well as possible.
