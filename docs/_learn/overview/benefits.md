---
layout: reference
title: Benefits of Coyote
section: learn
permalink: /learn/overview/benefits
---

## Key Benefits

Coyote helps solve the complexity of developing asynchronous software. It helps you with _design_ and _implementation_ by
providing high-level programming abstractions. It helps with _testing_ by providing support for writing detailed system 
[specifications](../specifications/overview) and a very effective high-coverage [testing tool](../tools/testing). 
The integration of these features into the same product helps you iterate faster
through the design-implement-test-revise cycle of software development. 

Coyote allows you to develop with confidence! That confidence comes from having your code changes be backed by powerful testing. This has allowed
teams to move at a faster pace, often finishing features in a fraction of the time compared to developing with legacy techniques where
the uncertainity of vetting corner-cases (especially with concurrency, failures, etc.) weighs you down heavily. The confidence also results in 
better performant code because Coyote helps you validate even highly-concurrent designs.

Testing with Coyote is designed to be easy and effective. It removes the mystery associated with finding concurrency bugs and the pain associated 
with debugging them.
Any bugs reported by the tester can be replayed: something that does not come
for free otherwise for programs with concurrency. Coyote testing can be integrated with standard unit-testing frameworks, or easily 
[parallelized](../tools/distributed-testing) for boosting coverage. It also makes it easy to [visualize](../tools/coverage) 
the coverage obtained for a test. 

Coyote is designed to be lightweight, adding minimal runtime overhead. It is easy to get started: simple design will have simple implementations,
and pay-as-you-go model for adding and testing more complex scenarios. 

Coyote has been [battle-tested by Azure](../../case-studies/azure-batch-service). There are several Azure services 
written using Coyote that are currently running live in production. We build on the experience of various teams that cover
a wide spectrum of requirements. 


