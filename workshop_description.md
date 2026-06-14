# Large Storage Locker

Large Storage Locker increases the capacity of the vanilla Storage Bin to:

1,000,000 t / 1,000,000,000 kg

What this mod changes:

- Only changes the vanilla Storage Bin capacity.
- Does not add a new building.
- Does not change building recipes, materials, power, decor, temperature behavior, automation, or storage filters.
- Does not modify Smart Storage Bin.
- Does not change any item, element, duplicant, or world generation data.

Compatibility:

- Uses a narrow Harmony postfix on `StorageLockerConfig.ConfigureBuildingTemplate`.
- Only raises `Storage.capacityKg` if the current value is lower than this mod's target capacity.
- If another mod sets the Storage Bin capacity higher, this mod will not overwrite it.
- Designed to be low-conflict with most mods because it does not replace building definitions or use transpilers.

Recommended load order:

- Usually does not matter.
- If you use another mod that also changes Storage Bin capacity, put the mod with your preferred final capacity later in the load order.

Large Storage Locker 会把原版储物箱的容量提高到：

1,000,000 吨 / 1,000,000,000 千克

这个模组改了什么：

- 只修改原版储物箱的容量。
- 不新增建筑。
- 不修改建筑配方、材料、电力、装饰、温度行为、自动化或储物筛选。
- 不修改智能储物箱。
- 不修改物品、元素、复制人或世界生成数据。

兼容性说明：

- 使用很窄的 Harmony Postfix，补丁目标是 `StorageLockerConfig.ConfigureBuildingTemplate`。
- 只有当当前储物箱容量低于本模组目标容量时，才会提高 `Storage.capacityKg`。
- 如果其他模组已经把储物箱容量设置得更高，本模组不会覆盖它。
- 没有替换建筑定义，也没有使用 transpiler，因此和大多数模组的冲突概率较低。

推荐加载顺序：

- 通常不需要特别调整。
- 如果你同时使用其他会修改储物箱容量的模组，请把你希望最终生效的那个模组放在更靠后的加载顺序。
