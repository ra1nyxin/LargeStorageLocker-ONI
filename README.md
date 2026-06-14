# Large Storage Locker

Large Storage Locker is an Oxygen Not Included mod that increases the capacity of the vanilla Storage Bin.

## Features

- Raises the vanilla Storage Bin capacity to `1,000,000,000 kg`.
- Equivalent to `1,000,000 t`.
- Keeps the change limited to `StorageLockerConfig.ConfigureBuildingTemplate`.
- Does not replace building definitions, recipes, UI, or storage logic.
- Only raises capacity when the current value is lower, so another mod can still set a larger value.

## Compatibility

The mod uses a narrow Harmony postfix and edits only the `Storage.capacityKg` value on the Storage Bin prefab.

This is intended to be compatible with most mods that do not fully replace the vanilla Storage Bin configuration.

## Build

Run:

```powershell
.\build.ps1
```

The build script compiles and installs the dev mod to:

```text
C:\Users\Administrator\Documents\Klei\OxygenNotIncluded\mods\Dev\LargeStorageLocker
```

## Workshop Package

Upload only the mod package files:

- `LargeStorageLocker.dll`
- `mod.yaml`
- `mod_info.yaml`
- optional preview image

Do not upload game DLLs from `OxygenNotIncluded_Data\Managed`.

## Notes

`Storage.capacityKg` is a `float`. Very large values are technically possible, but the current value is intentionally far below dangerous float limits while still being much larger than normal gameplay needs.
