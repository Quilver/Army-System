# Plan 01: Data Model, Stats, Equipment & Composite Resolution

**Goal:** Replace the current profile fields with the v0.9 stat set and add weapon and armour data, so later phases have
the values they need. Add the composite troop resolver and the tuning asset.

**Spec sections:** §1, §7.2, §7.3.
**Depends on:** G0 (Q-1, Q-2, Q-3, Q-6).
**Blocks:** all later phases.

**Status (2026-10-26):** Code complete. Compiles in Unity 6000.1.6f1 (batch, in a copy). Sample data generated and checked
against the sheets (0 failures). The repository's sample assets predate the integer change and must be regenerated from the
Editor. Old assets not migrated. See `Docs/Battle/Decisions.md`.

**Combat stat rules (D-22, D-23, D-24):** every combat stat is an integer 1–20, default 5. HP is an integer 10–40, default
15. Discipline is 1–20. Weapon skill levels are 1–20 when listed, and 0 means unlisted.

Legend: `[x]` done in code, `[~]` done with a documented change from the original task, `[ ]` pending (needs Unity or a
decision).

## Tasks

- [~] **Tuning asset.** `Model/TroopTuning.cs`, loaded from `Assets/Resources/Boid Profiles/Troop Tuning.asset`, with a
  code fallback. Holds only the Phase 01 values, not all of §7.2 (Decisions D-15). Later phases add theirs.
- [x] **Speed conversion.** `TroopTuning.SpeedUnitsPerStatPoint` (0.6). `SoldierProfile.MaxSpeed = Speed × 0.6`. The
  property keeps the name `MaxSpeed`, so steering and clash code need no change.
- [x] **Soldier stats.** `Model/SoldierProfile.cs`: `Speed`, `Defence`, `Strength`, `Support`, `Charge`, `Aggression`,
  `MaxHealth` (integer 10–40, default 15). `Discipline` removed (now on `TroopProfile`). Steering fields kept.
  - [~] `MeleeSkill` and `MeleeDamage` are **kept as legacy fields** so the current clash code compiles. Phase 04 removes
    them (Decisions D-13).
- [x] **Weapon skills.** `WeaponSkill` struct (`Model/WeaponSkill.cs`) in a list on `SoldierProfile`. `SkillFor(class)`
  returns 0 for an unlisted class. `OnValidate` warns above 2 entries.
- [~] **Loadout.** A list of up to 2 `WeaponProfile` (`Weapons`), plus `Armour`. `MeleeWeapon` and `RangedWeapon` are derived
  as the first of each type (Decisions D-04). The original fixed fields were replaced by the list, to match "up to 2 weapons"
  in the spec. A ranged soldier without a melee weapon logs a warning.
- [x] **`WeaponClass` enum** (`Model/WeaponTypes.cs`): Spear, Sword, Axe, Bow, Javelin, **Mace**, **Crossbow**. Added Mace and
  Crossbow (Decisions D-03, Q-52). `IsRanged()` extension.
- [x] **`WeaponProfile` ScriptableObject** (`Model/WeaponProfile.cs`, menu `Troops/Weapon Profile`). `Damage` is a single
  field: melee bonus or ranged base damage (Decisions D-05). Includes `ChargeBonus`, `Range`, `Accuracy`, `FireRate`,
  `SpecialRule` (`WeaponSpecialRule`: None, BoarSpearSupport, LanceChargeDouble) and a `Provisional` flag.
- [x] **`ArmourProfile` ScriptableObject** (`Model/ArmourProfile.cs`, menu `Troops/Armour Profile`). `Deflect`, `Bruise`,
  `Bleed`, `DefenceBonus`, `Provisional`. `IsValid` checks the sum is 100. `OnValidate` warns; it cannot block a save (D-16).
- [~] **Sample assets.** Created by `Troops > Phase 01 > Generate Sample Data` (`Editor/Phase01DataGenerator.cs`). The
  generator has not been run yet. It creates the spec samples (Short Sword, Lance, Longbow, Light and Heavy armour) plus
  the character weapons and armour from the designer's sheets. Values not on the sheets are provisional (Q-54, Q-55).
  Boar Spear is defined in code (`WeaponSpecialRule.BoarSpearSupport`) but no asset is generated, because no sheet uses it.
- [x] **`TroopProfile` changes** (`Model/TroopProfile.cs`): `BaseMorale` (1–20, default 5) replaces the absolute `Morale`.
  Discipline (integer 1–20, default 5) added. `LineWidth` kept until Phase 06.
  - [~] No `FormerlySerializedAs("_morale")`. The old value (10–100) is not carried over, so old assets reset to 5
    (Decisions D-10).
- [x] **Pure resolver** `Rules/CompositeTroopResolver.cs` with `TroopStatLine` and `ResolvedTroopStats`. Move = min,
  BaseMorale = max, Discipline = max, soldiers = sum. Empty input resolves to zeros.
  - Also added: `TroopProfile.CreateComposite(parts)`. It applies the resolver and builds the battle profile. Radius and line
    width take the largest part (Q-53).
- [x] **Move derivation.** `TroopProfile.Move` is the lowest soldier `Speed` in the composition. `Troop.Move` is the lowest over
  the roster. Nothing stores Move.
- [ ] **Migrate the four old TroopProfile assets** (Archer, Cavalry, Legion, Swordsmen) and the four old SoldierProfile
  assets. Pending: needs Unity and designer values (Next Steps 5). The sheet characters are added as new assets instead.
- [x] **Rename `InitialTroopCount`** to `InitialSoldierCount`: no code used it, so the name appears only in the spec and docs.
- [x] **`SoldierProfile.CreateRuntimeProfile` and `SetSpriteIfMissing`** are still used by `TroopSoldierShowcase` and
  `TroopProfileSpriteDefaults`, so they stay.

## Files

- New: `Model/WeaponTypes.cs`, `Model/WeaponSkill.cs`, `Model/WeaponProfile.cs`, `Model/ArmourProfile.cs`,
  `Model/TroopTuning.cs`, `Rules/CompositeTroopResolver.cs`, `Editor/Phase01DataGenerator.cs`.
- Changed: `Model/SoldierProfile.cs`, `Model/TroopProfile.cs`, `Runtime/Troop.cs` (`Move`, `Discipline`),
  `Runtime/TroopRoster.cs` (`MinProfileValue`), `Runtime/TroopMorale.cs` (`Value` = BaseMorale × living),
  `Combat/MeleeAttackClock.cs` (troop Discipline), `Runtime/TroopConstants.cs` (comment only),
  `View/TroopSoldierShowcase.cs` (morale 100 → 5).
- Generated assets (after the generator runs): `Assets/Resources/Boid Profiles/Troop Tuning.asset`,
  `Characters/Soldiers/*.asset` (11), `Characters/Troops/*.asset` (11), `Equipment/Weapons/*.asset` (19),
  `Equipment/Armour/*.asset` (7).

## Acceptance Criteria

- [x] Compile-checked outside Unity (stub, 0 errors, 0 warnings).
- [x] Unity batch compile in a copy of the project: exit 0, no `error CS` lines.
- [x] Generator run in the copy: 11 characters, 19 weapons, 7 armour, 0 out-of-range sheet values.
- [x] Saved-asset check (copy): 0 failures. All stats, HP, skills and troop lines match the sheets.
- [ ] Assets import with no errors in the repository. The Console is clean in `Level 1 Troop Test`. (Editor; Decisions Next Steps 1)
- [x] Composite resolver checks pass: `min` Move, `max` morale and discipline, summed counts, single part resolves to itself,
  empty input is zero (`RunCheck`, 25 checks total, all passing).
- [ ] EditMode tests in the repo (`CompositeTroopResolverTests`). Blocked on the test framework (Next Steps 8).
- [x] Every stat in `specifications.md` §1.4–1.5 has a field. Labels come from the field names and `Header` attributes.
- [x] `ArmourProfile` reports an invalid split (`IsValid`) and logs a warning. It cannot block the save (D-16).
- [x] `SoldierProfile` warns when it has more than 2 weapon skills (or more than 2 weapons).
- [x] No reads of `Discipline` on `SoldierProfile` remain (grep).

## Risks

- `MaxHealth` default changed from 100 to 15 (range 10–40). The old assets keep their own values, but new profiles are much
  more lethal. Re-tune in Phase 02.
- Morale rescale from 10–100 to 1–20 changes every break threshold. The old Flock assets reset to 5 until they are migrated.
  Keep a before/after table with the migration.
- The legacy clash inputs (`MeleeSkill`, `MeleeDamage`) are approximations of the new stats. Battle balance changes until
  Phase 04.
