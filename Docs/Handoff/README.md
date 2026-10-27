# Handoff Documents: Troop Movement System

Work packages for testing and fixing the troop movement and combat systems in `Assets/Battle Scripts/Troop/`. Read
`Assets/Battle Scripts/Troop/README.md` first for the architecture and per-frame update order.

Nothing in these documents has been compiled or run. Code locations are given as `file:line` against the code at the
time of writing; check them before relying on them.

## Documents

| Document                                                                                 | Type                                 | Status                | Depends on                      |
| ---------------------------------------------------------------------------------------- | ------------------------------------ | --------------------- | ------------------------------- |
| [movement-testing.md](movement-testing.md)                                               | Logging infrastructure and test pass | Not started           | -                               |
| [issue-01-chain-exit-controls.md](issue-01-chain-exit-controls.md)                       | Known issue                          | Not confirmed         | Testing (Chain, Pivot logging)  |
| [issue-02-flank-snapping-breaks-surround.md](issue-02-flank-snapping-breaks-surround.md) | Known issue                          | Not confirmed         | Testing (Chain, Pivot logging)  |
| [issue-03-retreat-overshoot-yoyo.md](issue-03-retreat-overshoot-yoyo.md)                 | Known issue                          | Not confirmed         |                                 |
| [issue-04-idle-engaged-fallback.md](issue-04-idle-engaged-fallback.md)                   | Known issue (diagnosis)              | Suspected cause found |                                 |
| [barrier-intrusion-tracking.md](barrier-intrusion-tracking.md)                           | Fix                                  | Completed             | Issue 04                        |
| [issue-05-clash-without-contact.md](issue-05-clash-without-contact.md)                   | Known issue (design decision)        | Not confirmed         | Testing (Clash logging)         |
| [issue-06-lazy-chargers.md](issue-06-lazy-chargers.md)                                   | Known issue                          | Not confirmed         | Testing (Charge, Clash logging) |
| [issue-07-missing-charge-support.md](issue-07-missing-charge-support.md)                 | Missing feature                      | Baseline needed       | Testing (Charge, Clash logging) |

## Suggested order

1. Build the diagnostics in `movement-testing.md`. Every issue document relies on its logging categories.
2. Confirm issue 04, then implement `barrier-intrusion-tracking.md`. It is the best-understood bug.
3. Run the feature and edge-case test pass, and record the baseline for issues 03, 05, 06 and 07.
4. Work through the remaining issues. Re-run the affected rows of the test matrix after each fix.

## Conventions for every issue document

Each issue document has the same sections: Summary, Symptom, Repro, Suspected cause, Logging needed, Confirmation
criteria, Fix direction, Verification. Update its status in the table above when it is confirmed, ruled out or
fixed, and attach the CSVs or screenshots used as evidence.

## Project rules that apply

- Unity `6000.1.6f1`. There is no CLI build or test; verify in the Editor and check the Console after compiling.
- Edit scenes, prefabs and ScriptableObjects through Unity, and keep `.meta` files.
- `PlayerInputMap.cs` is generated. Change `PlayerInputMap.inputactions` instead.
- `ChargeCoordinator` and `ClashResolver` use `UnityEngine.Random`, so runs are not reproducible unless a seed is set
  (see the diagnostics seed option).
