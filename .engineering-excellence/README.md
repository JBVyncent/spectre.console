# Agentic Engineering Excellence Pack

This folder defines merge-blocking, evidence-first quality contracts for agentic development.

## Required PR Artifacts

- `change-contract.json`: Declares intended behavior, invariants, risk, rollback, and gate requirements.
- `evidence-bundle.json`: Records gate results and supporting evidence.

## Schemas

- `schemas/change-contract.schema.json`
- `schemas/evidence-bundle.schema.json`

## Validator

Run:

```bash
python3 tools/ci/agentic_gatekeeper.py \
  --contract .engineering-excellence/change-contract.json \
  --evidence .engineering-excellence/evidence-bundle.json
```

The validator enforces outcome gates and rejects anthropomorphic merge blockers (cyclomatic complexity, LOC, style-score heuristics).
