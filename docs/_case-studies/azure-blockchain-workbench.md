---
layout: reference
title: Azure Blockchain Workbench case study
section: case-studies
permalink: /case-studies/azure-blockchain-workbench
---

# Azure Blockchain Workbench

## Background

[Azure Blockchain Workbench](https://azure.microsoft.com/en-in/features/blockchain-workbench/)
is a packaged solution that allows users to spin up end-to-end blockchain applications integrated
with a number of Azure services with minimal effort on their part.
The users provide Workbench with an Ethereum smart contract
and associated metadata, and Workbench spins up the requisite Azure
infrastructure. Users can authenticate and sign
transactions using their AAD identities, submit transactions into
the blockchain and consume events and transactions from the
blockchain through the event grid as well as through a SQL database
with no additional development effort on their part.

## Challenge

Ethereum Transaction Submitter is the component within Workbench
responsible for submitting transactions into the
blockchain. Blockchains are distributed systems where
user-submitted transactions are eventually included (or _mined_) into
the blockchain. The node through which the transaction is
submitted is typically not the one that mines it and can, in fact,
even die before gossiping about the transaction to the rest of the network.
This requires periodic resubmissions of transactions if they're not
mined. Blockchains also exhibit _forks_ where history can be
rewound. In that case, transactions that were successfully mined previously need to be submitted
again. Writing a high-throughput service that can reliably submit
transactions into the blockchain is thus a harder problem than it first
appears.

## Solution and Coyote's key advantages

An initial version of the Ethereum Transaction Submitter was facing a
number of reliability issues and missed corner cases, so the team decided to write the service using
Coyote. The use of Coyote helped the team in a number of ways.

- Coyote safety conditions force the user to think about what must be
  true in all states (aka _invariants_) as opposed to the ways in
  which the system can fail. This helped the team gain clarity in their design.

- Coyote helped find and fix a liveness condition that would have
  been near _impossible_ to detect without Coyote's testing methodology.
  Blockchains can occasionally fork, which was causing an initial version of the service
  to permanently stall. Without a means of reproducing forks in a controlled way, such
  issues are hard to debug. The Azure team implemented a mock
  for the blockchain network using Coyote and were then able to systematically
  introduce a fork during testing. Coyote was able to detect the liveness bug, a fix was
  implemented, after which Coyote tests passed. As a result, the team
  had a much higher confidence in deploying their fix.

- Developing the system using Coyote's state machine programming model
  led to a highly concurrent and performant implementation.

- Coyote allowed the team to concisely state the various success and failure
  conditions in the individual mocks, leaving the job of exploration to the tester.
  This allowed the team to effectively have a very large number of (traditional) test
  cases without having to write them all by hand.

- Coyote's deterministic repro of bugs allowed the team to debug and
  understand the bugs much more easily than before.
