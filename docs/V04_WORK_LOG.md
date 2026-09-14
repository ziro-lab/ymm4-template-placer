# v0.4 work log

## W1: frozen regression

Base main: fd2c2bfebdbf736d47c9f591290d162c68e4e178.
Native baseline: run 34842291705, source f716199c8f936bebcfa032f207504416f2b2df6c.
63 native assertions, P1-P9, distribution smoke and package PASS.
The difference from that source to base main is limited to AGENTS.md, README.md and docs/DESIGN.md.

## W2: add-only plan foundation

All selected outputs are independently cloned and full-span checked against existing and already planned items before a single native Items swap. Empty assignment is a no-op. No product delete/rebuild path remains.
The old P8 delete-empty-assignment expectation is intentionally replaced by preservation. Golden-path fixture changes explicitly delete old test items through the native host before testing a new assignment; this is not product behavior.
Native proof pending for this commit.
