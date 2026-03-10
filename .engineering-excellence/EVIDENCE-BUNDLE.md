# Evidence Bundle Format

`evidence-bundle.json` is the merge-time proof artifact consumed by `agentic_gatekeeper.py`.

## Required Top-Level Fields

- `bundle_version` (`"1.0"`)
- `generated_at_utc` (ISO-8601 UTC timestamp)
- `contract_path` (path to `change-contract.json`)
- `gates` (object containing `unit`, `integration`, `mutation`)

## Gate Object

Each gate includes:

- `status`: `PASS`, `FAIL`, or `SKIPPED`
- `command`: exact command used
- `tfms`: targeted frameworks where applicable
- `evidence`: `{ "type": "ci_link" | "command_summary", "value": "..." }`
- `waiver` (required when skipped): `{ "issue", "owner", "expires_on", "reason" }`

## Mutation Gate

When status is `PASS`, include:

- `score` (numeric, must be >= `change-contract.acceptance_gates.mutation.minimum_score`)

## Waiver Rules

- Any skipped required gate must include a waiver.
- Waivers must include issue, owner, and expiry date.
- Expired waivers fail validation.
