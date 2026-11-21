# Large Storage Locker

Large Storage Locker is an Oxygen Not Included mod that increases the capacity of the vanilla Storage Bin.

## Features

- Raises the vanilla Storage Bin capacity to `1,000,000,000 kg`.
- Equivalent to `1,000,000 t`.
- Keeps the change limited to `StorageLockerConfig.ConfigureBuildingTemplate`.
- Does not replace building definitions, recipes, UI, or storage logic.
- Only raises capacity when the current value is lower, so another mod can still set a larger value.

## Compatibility

The mod applies three independent sets of patches:

1. A narrow Harmony postfix on `StorageLockerConfig.ConfigureBuildingTemplate` that edits only `Storage.capacityKg` on the Storage Bin prefab.
2. A `Storage.MassStored()` postfix that corrects overflow when a storage contains more than about 2,147 t. It applies to all storage buildings, including existing ones, so remaining capacity and fullness checks also receive the corrected mass.
3. The fetch layer guards described below, which change no values at all.

This is intended to be compatible with most mods that do not fully replace the vanilla Storage Bin configuration.

The mass fix only recalculates when the original result is negative and replaces it only if the actual mass is in the overflow range. Normal storage calls retain the original result. Large masses remain subject to the game's `float` precision.

## Fetch Layer Guards

`FetchChore` registers itself in a `GameScenePartitioner` layer bucket from its constructor and unregisters in `Cleanup()`. Tearing an entry down frees the handle, nulls the entry's `obj` and returns the entry object to a shared pool, but a query validates an entry only on its bounds and query id, never on its layer or its `obj`. A chore consumer can therefore be handed a null or already recycled entry.

Both vanilla consumers of that layer assume they only ever receive a `FetchChore`, and one of them reinterprets the entry without checking at all. Vanilla already asserts on exactly these conditions ("FindNextChore found an entry that wasn't a FetchChore"), which shows the engine expects a stale entry to be possible; it just has no recovery path for one.

| Guard | Target | What it does |
| --- | --- | --- |
| Solid transfer arm chore scan | `ChoreConsumer`'s chore evaluator | Drops an entry that is not a live `FetchChore` with a live target, then continues the scan |
| Nearby chore gathering | `FetchAreaChore.gatherNearbyFetchChoresVisitor` | Drops an entry that is not a live `FetchChore`, then continues the scan |
| Assert backstop | `DebugUtil.Assert(bool, string)` | Suppresses only the `FindNextChore found an entry ...` messages |

Every guard resolves its target by reflection, validates the exact signature before patching, and fails open with a message in `Player.log`. If a game update moves or reshapes one of these methods, that guard is not installed and the rest of the mod keeps working.

The guards change no game rule, balance value, element or storage amount. They only make the chore system drop an entry it cannot act on, which is what the vanilla asserts already declare such an entry to be. Dropped entries are logged (the first 5 and then every 500th) so a report can be diagnosed from `Player.log` alone.

This matters for a 1,000,000 t bin because it moves mass in bursts three orders of magnitude larger than vanilla, creating and destroying far more `Pickupable`/`FetchChore` pairs, so a window that is normally almost impossible to hit is reached routinely. The guards do not depend on knowing why an entry went stale; they only refuse to act on one that is.

## Build

Run:

```powershell
.\build.ps1
```

The build script compiles every `.cs` file under `src\` and installs the dev mod to the current user's Klei mod folder:

```text
%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Dev\LargeStorageLocker
```

Edit `$devModDir` at the top of `build.ps1` if the game keeps mods somewhere else.

## Workshop Package

Upload only the mod package files:

- `LargeStorageLocker.dll`
- `mod.yaml`
- `mod_info.yaml`
- optional preview image

Do not upload game DLLs from `OxygenNotIncluded_Data\Managed`.

## Notes

`Storage.capacityKg` is a `float`. Very large values are technically possible, but the current value is intentionally far below dangerous float limits while still being much larger than normal gameplay needs.
