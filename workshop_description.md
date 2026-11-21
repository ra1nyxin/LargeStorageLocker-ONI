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
- Fixes `Storage.MassStored()` overflowing above about 2,147 t for storage buildings, including existing saves.
- Only raises `Storage.capacityKg` if the current value is lower than this mod's target capacity.
- If another mod sets the Storage Bin capacity higher, this mod will not overwrite it.
- Designed to be low-conflict with most mods because it does not replace building definitions or use transpilers.

Stability:

- Adds three narrow Harmony prefixes that make the chore system drop a stale scene partitioner entry instead of asserting on it. This covers a crash that can happen when very large amounts of material are pushed through storage filters at once.
- These guards change no values and no game rules. If a game update moves the methods they watch, they simply do not install themselves and are reported in `Player.log`.

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
- 修复储存建筑的物品总质量超过约 2147 吨时发生的数值溢出，已有存档同样生效。
- 只有当当前储物箱容量低于本模组目标容量时，才会提高 `Storage.capacityKg`。
- 如果其他模组已经把储物箱容量设置得更高，本模组不会覆盖它。
- 没有替换建筑定义，也没有使用 transpiler，因此和大多数模组的冲突概率较低。

稳定性说明：

- 增加了三个很窄的 Harmony Prefix，让搬运系统丢弃场景分区里的残留条目，而不是直接断言失败。这覆盖了一个在极大量物质同时流经储物筛选时可能出现的崩溃。
- 这些防护不修改任何数值，也不修改任何游戏规则。如果游戏更新挪动了它们关注的函数，它们只会不安装并把原因写进 `Player.log`，其余功能不受影响。

推荐加载顺序：

- 通常不需要特别调整。
- 如果你同时使用其他会修改储物箱容量的模组，请把你希望最终生效的那个模组放在更靠后的加载顺序。
